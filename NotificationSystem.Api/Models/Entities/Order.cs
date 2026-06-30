using System.Collections.ObjectModel;
using Microsoft.VisualBasic;

namespace NotificationSystem.Api.Models.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<NotificationAttempt> NotificationAttempts { get; set; } = new();
}