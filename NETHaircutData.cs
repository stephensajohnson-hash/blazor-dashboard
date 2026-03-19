using System;
using System.Collections.Generic;

namespace Dashboard
{
    public class NETH_Phone
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Type { get; set; } // Mobile, Work, etc.
        public bool IsBadNumber { get; set; } // The "Bad Number" flag you requested
    }

    public class NETH_Shop
    {
        public int Id { get; set; }
        public string UrlKey { get; set; } 
        public string BusinessName { get; set; }
        public string BrandColor { get; set; } = "#D2B48C"; 
        
        // Use MediaId instead of a string URL
        public int? LogoMediaId { get; set; }
        public NETH_Media Logo { get; set; }

        public string StripeAccountId { get; set; } // For Shop-level payments

        public int PhoneId { get; set; }
        public NETH_Phone Phone { get; set; }

        public List<NETH_Stylist> Stylists { get; set; } = new();
    }

    public class NETH_Stylist
    {
        public int Id { get; set; }
        public int ShopId { get; set; }
        public NETH_Shop Shop { get; set; } // Added this back in case it's needed for the Include
        
        public string Name { get; set; }
        public string Bio { get; set; }
        public bool IsIndependent { get; set; }
        public string StripeAccountId { get; set; }

        public int PhoneId { get; set; }
        public NETH_Phone Phone { get; set; }

        // THIS IS THE MISSING LINE:
        public List<NETH_Service> Services { get; set; } = new(); 
    }

    public class NETH_Client
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int PhoneId { get; set; }
        public NETH_Phone Phone { get; set; }
    }

    public class NETH_ClientNote
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int StylistId { get; set; }
        public int? ServiceId { get; set; } // Link to the specific haircut/service
        
        // The Media link for photos of haircuts or formulas
        public int? MediaId { get; set; }
        public NETH_Media AttachedMedia { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }

        public int? EventYear { get; set; }
        public int? EventMonth { get; set; }
        public int? EventDay { get; set; }

        public List<NETH_NoteTag> Tags { get; set; } = new();
    }

    public class NETH_Media
    {
        public int Id { get; set; }
        public string FileUrl { get; set; }
        public string Category { get; set; } // "Logo", "Formula", "Portfolio", "ClientPhoto"
        public string Description { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }

    public class NETH_Service
    {
        public int Id { get; set; }
        public int StylistId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class NETH_NoteTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string HexColor { get; set; }
    }

    public class NETH_Appointment
    {
        public int Id { get; set; }
        public int ShopId { get; set; }
        public int StylistId { get; set; }
        public int ClientId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; } // Calculated by summing all service durations
        public string Status { get; set; } = "Booked";

        // Instead of a single ServiceId, we use this List
        public List<NETH_AppointmentService> SelectedServices { get; set; } = new();

        public decimal TotalPrice { get; set; } 
        public string Notes { get; set; }
        public bool IsPaid { get; set; }

        public NETH_Client Client { get; set; }
        public NETH_Stylist Stylist { get; set; }
    }

    public class NETH_AppointmentService
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; } // Foreign Key
        public int ServiceId { get; set; }     // Foreign Key
        
        // We store the price/duration at the time of booking 
        // in case the stylist changes their rates later.
        public decimal PriceAtBooking { get; set; }
        public int DurationAtBooking { get; set; }

        public NETH_Service Service { get; set; }
    }
}