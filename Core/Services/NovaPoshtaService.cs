using System;
using System.Text;
using AutoMapper;
using Core.Interfaces;
using Core.Models.NovaPoshta.Area;
using Core.Models.NovaPoshta.City;
using Core.Models.NovaPoshta.Department;
using Domain;
using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Core.Services;

public class NovaPoshtaService(IMapper mapper, IHttpClientFactory httpClientFactory, AppDbPizushiContext context, IServiceScopeFactory scopeFactory) : INovaPoshtaService
{
    private readonly HttpClient http = httpClientFactory.CreateClient();
    private readonly string url = "https://api.novaposhta.ua/v2.0/json/";
    private readonly string apiKey = "c0211966681949ebe4c2ca33bb99f1b7";

    private async Task<List<AreaItemResponse>> FetchAreasAsync()
    {
        var request = new AreaPostModel
        {
            ApiKey = apiKey,
            ModelName = "Address",
            CalledMethod = "getAreas",
            MethodProperties = new MethodProperties()
        };

        var result = await SendRequestAsync<AreaResponse>(request);

        return result?.Success == true && result.Data != null
            ? result.Data
            : new List<AreaItemResponse>();
    }

    public async Task<List<CityItemResponse>> FetchCitiesAsync()
    {
        var areas = await FetchAreasAsync();
        var cities = new List<CityItemResponse>();

        foreach (var area in areas)
        {
            var modelRequest = new CityPostModel
            {
                ApiKey = apiKey,
                MethodProperties = new MethodCityProperties { AreaRef = area.Ref }
            };

            var result = await SendRequestAsync<CityResponse>(modelRequest);
            if (result?.Data != null && result.Success)
            {
                foreach (var city in result.Data)
                {

                    var existingCity = await context.Cities
                      .FirstOrDefaultAsync(c => c.Ref == city.Ref);

                    if (existingCity == null)
                    {
                        var entity = mapper.Map<CityEntity>(city);
                        entity.Ref = city.Ref;
                        entity.Name = city.Description;

                        await context.Cities.AddAsync(entity);
                        Console.WriteLine($"Add city: {city.Description}");
                    }

                }

                cities.AddRange(result.Data);
            }
        }

        await context.SaveChangesAsync();
        return cities;
    }

    public async Task<List<DepartmentItemResponse>> FetchDepartmentsAsync()
    {
        var departments = new List<DepartmentItemResponse>();

        var cities = await context.Cities.ToListAsync();

        var semaphore = new SemaphoreSlim(5);

        var tasks = cities.Select(async cityEntity =>
        {
            await semaphore.WaitAsync();
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<AppDbPizushiContext>();

                var modelRequest = new DepartmentPostModel
                {
                    ApiKey = apiKey,
                    MethodProperties = new MethodDepatmentProperties { CityRef = cityEntity.Ref }
                };

                var result = await SendRequestAsync<DepartmentResponse>(modelRequest);

                if (result?.Data != null && result.Success)
                {
                    foreach (var department in result.Data)
                    {
                        var exists = await scopedContext.PostDepartments
                          .AnyAsync(d => d.Ref == department.Ref && d.CityId == cityEntity.Id);

                        if (!exists)
                        {
                            var entity = mapper.Map<PostDepartmentEntity>(department);
                            entity.CityId = cityEntity.Id;
                            entity.Ref = department.Ref; // зберігаємо унікальний код з API
                            entity.Name = department.Description; // зберігаємо назву

                            await scopedContext.PostDepartments.AddAsync(entity);
                            Console.WriteLine($"Add post department: {department.Description}");
                        }
                    }
                    await scopedContext.SaveChangesAsync();

                    lock (departments)
                    {
                        departments.AddRange(result.Data);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return departments;
    }

    private async Task<T?> SendRequestAsync<T>(object modelRequest) where T : class
    {
        string json = JsonConvert.SerializeObject(modelRequest, new JsonSerializerSettings
        {
            ContractResolver = (modelRequest is AreaPostModel)
                ? new CamelCasePropertyNamesContractResolver()
                : null,
            Formatting = Formatting.Indented
        });

        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await http.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            string jsonResp = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonResp);
        }

        return null;
    }
}
