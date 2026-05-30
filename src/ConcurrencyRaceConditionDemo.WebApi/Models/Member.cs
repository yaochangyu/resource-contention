using System.ComponentModel.DataAnnotations;

namespace ConcurrencyRaceConditionDemo.WebApi.Models;

public class Member
{
    public int Id { get; set; }
    public int Points { get; set; }
    
    [ConcurrencyCheck]
    public int Version { get; set; }
}
