using System.ComponentModel.DataAnnotations.Schema;

namespace Spot4Hire.Backend.Data.Entities.Common
{
    public interface ISoftDeletable
    {
        DateTime? DeletedAt { get; set; }

        [NotMapped]
        bool IsDeleted => DeletedAt is not null;
    }
}
