﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Delivery
{
    [Table("tbl_DeliveryInfos")]
    public class DeliveryInfoEntity
    {
        [Key]
        [ForeignKey("Order")]
        public long? OrderId { get; set; }
        public OrderEntity? Order { get; set; }

        //[ForeignKey("City")]
        //public long CityId { get; set; }
        //public CityEntity City { get; set; } = null!;

        [ForeignKey("PostDepartment")]
        public long PostDepartmentId { get; set; }
        public PostDepartmentEntity PostDepartment { get; set; } = null!;

        [ForeignKey("PaymentType")]
        public long PaymentTypeId { get; set; }
        public PaymentTypeEntity PaymentType { get; set; } = null!;

        public string PhoneNumber { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
    }
}
