using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wirehome.Core.History.Repository.Entities
{
    [Table("ComponentStatus")]
    public class ComponentStatusEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint ID { get; set; } 

        [StringLength(256)]
        public string ComponentUid { get; set; }

        [StringLength(256)]
        public string StatusUid { get; set; }

        [StringLength(1024)]
        public string Value { get; set; }

        public DateTimeOffset RangeStart { get; set; }

        public DateTimeOffset RangeEnd { get; set; }

        public bool IsLatest { get; set; }

        public uint? PredecessorID { get; set; }

        [ForeignKey("PredecessorID")]
        public ComponentStatusEntity Predecessor { get; set; }
    }
}
