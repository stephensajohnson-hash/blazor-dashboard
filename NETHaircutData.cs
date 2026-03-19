using System;
using System.Collections.Generic;

namespace Dashboard;

public class NETH_Phone
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "Mobile";
    public bool IsBadNumber { get; set; }
}

public class NETH_Shop
{
    public int Id { get; set; }
    public string UrlKey { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string BrandColor { get; set; } = "#D2B48C";
    public int? LogoMediaId { get; set; }
    public NETH_Media? Logo { get; set; }
    public string StripeAccountId { get; set; } = "";
    public int PhoneId { get; set; }
    public NETH_Phone Phone { get; set; } = null!;
    public List<NETH_Stylist> Stylists { get; set; } = new();
}

public class NETH_Stylist
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public NETH_Shop Shop { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Bio { get; set; } = "";
    public bool IsIndependent { get; set; }
    public string StripeAccountId { get; set; } = "";
    public int PhoneId { get; set; }
    public NETH_Phone Phone { get; set; } = null!;
    public List<NETH_Service> Services { get; set; } = new();
}

public class NETH_Service
{
    public int Id { get; set; }
    public int StylistId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
}

public class NETH_Client
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int PhoneId { get; set; }
    public NETH_Phone Phone { get; set; } = null!;
}

public class NETH_ClientNote
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int StylistId { get; set; }
    public int? ServiceId { get; set; }
    public int? MediaId { get; set; }
    public NETH_Media? AttachedMedia { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string ShortDescription { get; set; } = "";
    public string LongDescription { get; set; } = "";
    public int? EventYear { get; set; }
    public int? EventMonth { get; set; }
    public int? EventDay { get; set; }
    public List<NETH_NoteTag> Tags { get; set; } = new();
}

public class NETH_Media
{
    public int Id { get; set; }
    public string FileUrl { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UploadedDate { get; set; } = DateTime.Now;
}

public class NETH_NoteTag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string HexColor { get; set; } = "#000000";
}

public class NETH_Appointment
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public int StylistId { get; set; }
    public int ClientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Booked";
    public List<NETH_AppointmentService> SelectedServices { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string Notes { get; set; } = "";
    public bool IsPaid { get; set; }
}

public class NETH_AppointmentService
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }
    public decimal PriceAtBooking { get; set; }
    public int DurationAtBooking { get; set; }
}