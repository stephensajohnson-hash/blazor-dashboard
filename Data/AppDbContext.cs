using Microsoft.EntityFrameworkCore;

namespace Dashboard
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // General / Legacy Tables
        public DbSet<User> Users { get; set; }
        public DbSet<LinkGroup> LinkGroups { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<Countdown> Countdowns { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<StoredImage> StoredImages { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
        public DbSet<RecipeCategory> RecipeCategories { get; set; }

        // Bullet Calendar Core
        public DbSet<BulletItem> BulletItems { get; set; }
        public DbSet<BulletItemNote> BulletItemNotes { get; set; }
        public DbSet<BulletTaskDetail> BulletTaskDetails { get; set; }
        public DbSet<BulletMeetingDetail> BulletMeetingDetails { get; set; }
        public DbSet<BulletHabitDetail> BulletHabitDetails { get; set; }
        public DbSet<BulletMediaDetail> BulletMediaDetails { get; set; }
        public DbSet<BulletHolidayDetail> BulletHolidayDetails { get; set; }
        public DbSet<BulletBirthdayDetail> BulletBirthdayDetails { get; set; }
        public DbSet<BulletAnniversaryDetail> BulletAnniversaryDetails { get; set; }
        public DbSet<BulletVacationDetail> BulletVacationDetails { get; set; }
        public DbSet<BulletTaskTodoItem> BulletTaskTodoItems { get; set; }
        public DbSet<BulletHealthDetail> BulletHealthDetails { get; set; }
        public DbSet<BulletHealthMeal> BulletHealthMeals { get; set; }
        public DbSet<BulletHealthWorkout> BulletHealthWorkouts { get; set; }

        // Sports & Budget
        public DbSet<League> Leagues { get; set; }
        public DbSet<Season> Seasons { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<BulletGameDetail> BulletGameDetails { get; set; }
        public DbSet<BudgetPeriod> BudgetPeriods { get; set; }
        public DbSet<BudgetCycle> BudgetCycles { get; set; }
        public DbSet<BudgetItem> BudgetItems { get; set; }
        public DbSet<BudgetTransaction> BudgetTransactions { get; set; }
        public DbSet<BudgetTransactionSplit> BudgetTransactionSplits { get; set; }
        public DbSet<BudgetTransfer> BudgetTransfers { get; set; }
        public DbSet<BudgetIncomeSource> BudgetIncomeSources { get; set; }
        public DbSet<BudgetExpectedIncome> BudgetExpectedIncome { get; set; }
        public DbSet<BudgetWatchItem> BudgetWatchItems { get; set; }
        public DbSet<HsaReceipt> HsaReceipts { get; set; }
        public DbSet<HsaDisbursement> HsaDisbursements { get; set; }
        public DbSet<PtoPolicy> PtoPolicies { get; set; }
        public DbSet<PtoEntry> PtoEntries { get; set; }

        // NET Haircut
        public DbSet<NETH_Phone> NETH_Phones { get; set; }
        public DbSet<NETH_Shop> NETH_Shops { get; set; }
        public DbSet<NETH_Stylist> NETH_Stylists { get; set; }
        public DbSet<NETH_Service> NETH_Services { get; set; }
        public DbSet<NETH_Client> NETH_Clients { get; set; }
        public DbSet<NETH_ClientNote> NETH_ClientNotes { get; set; }
        public DbSet<NETH_NoteTag> NETH_NoteTags { get; set; }
        public DbSet<NETH_Media> NETH_MediaItems { get; set; }
        public DbSet<NETH_Appointment> NETH_Appointments { get; set; }
        public DbSet<NETH_AppointmentService> NETH_AppointmentsService { get; set; }
        public DbSet<NETH_Schedule> NETH_Schedules { get; set; }
        public DbSet<NETH_StoredImage> NETH_StoredImages { get; set; }

        // PickPrepPlate (PPP)
        public DbSet<PPP_User> PPP_Users { get; set; }
        public DbSet<PPP_StoredImage> PPP_StoredImages { get; set; }
        public DbSet<PPP_Owner> PPP_Owners { get; set; }
        public DbSet<PPP_Address> PPP_Addresses { get; set; }
        public DbSet<PPP_Ingredient> PPP_Ingredients { get; set; }
        public DbSet<PPP_Recipe> PPP_Recipes { get; set; }
        public DbSet<PPP_RecipeIngredientGroup> PPP_RecipeIngredientGroups { get; set; }
        public DbSet<PPP_RecipeIngredientMapping> PPP_RecipeIngredientMappings { get; set; }
        public DbSet<PPP_RecipeInstructionGroup> PPP_RecipeInstructionGroups { get; set; }
        public DbSet<PPP_RecipeInstruction> PPP_RecipeInstructions { get; set; }
        public DbSet<PPP_Menu> PPP_Menus { get; set; }
        public DbSet<PPP_MenuItem> PPP_MenuItems { get; set; }
        public DbSet<PPP_MenuItemTimeframe> PPP_MenuItemTimeframes { get; set; }
        public DbSet<PPP_MenuItemSize> PPP_MenuItemSizes { get; set; }
        public DbSet<PPP_MenuItemOption> PPP_MenuItemOptions { get; set; }
        public DbSet<PPP_Order> PPP_Orders { get; set; }
        public DbSet<PPP_OrderItem> PPP_OrderItems { get; set; }
        public DbSet<PPP_OrderItemOption> PPP_OrderItemOptions { get; set; }
        public DbSet<PPP_DeliveryZipCode> PPP_DeliveryZipCodes { get; set; }
        public DbSet<PPP_PickupLocation> PPP_PickupLocations { get; set; }
        public DbSet<PPP_UserAddress> PPP_UserAddresses { get; set; }
        public DbSet<PPP_DeliveryRadiusRule> PPP_DeliveryRadiusRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Force EF to use our manual column names for PPP Menu Engine
            modelBuilder.Entity<PPP_MenuItem>()
                .HasOne<PPP_Menu>()
                .WithMany(m => m.Items)
                .HasForeignKey(i => i.MenuId);

            modelBuilder.Entity<PPP_MenuItemTimeframe>()
                .HasOne<PPP_MenuItem>()
                .WithMany(i => i.Timeframes)
                .HasForeignKey(t => t.MenuItemId);

            modelBuilder.Entity<PPP_MenuItemSize>()
                .HasOne<PPP_MenuItem>()
                .WithMany(i => i.Sizes)
                .HasForeignKey(s => s.MenuItemId);

            modelBuilder.Entity<PPP_MenuItemOption>()
                .HasOne<PPP_MenuItemSize>()
                .WithMany(s => s.Options)
                .HasForeignKey(o => o.MenuItemSizeId);

            modelBuilder.Entity<PPP_OrderItem>()
                .HasOne<PPP_Order>()
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<PPP_OrderItemOption>()
                .HasOne<PPP_OrderItem>()
                .WithMany(i => i.SelectedOptions)
                .HasForeignKey(o => o.OrderItemId);

            modelBuilder.Entity<PPP_OrderItem>()
                .HasOne(i => i.ParentOrderContainer)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId);
        }
    }
}