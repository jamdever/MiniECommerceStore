using Microsoft.EntityFrameworkCore;
namespace MiniECommerceStore.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole composite key
            modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserID, ur.RoleID });
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserID);
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleID);

            // Product -> Category relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting category if products exist

            // Order -> User relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if orders exist

            // Order -> OrderItem relationship
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> Product relationship
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting product if in orders

            // Basket -> User relationship
            modelBuilder.Entity<Basket>()
                .HasOne(b => b.User)
                .WithMany(u => u.Baskets)
                .HasForeignKey(b => b.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Basket -> BasketItem relationship
            modelBuilder.Entity<Basket>()
                .HasMany(b => b.Items)
                .WithOne(bi => bi.Basket)
                .HasForeignKey(bi => bi.BasketID)
                .OnDelete(DeleteBehavior.Cascade);

            // BasketItem -> Product relationship
            modelBuilder.Entity<BasketItem>()
                .HasOne(bi => bi.Product)
                .WithMany()
                .HasForeignKey(bi => bi.ProductID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting product if in baskets

            // Review -> Product relationship
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductID)
                .OnDelete(DeleteBehavior.Cascade); // Delete reviews when product is deleted

            // Review -> User relationship
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if reviews exist

            // Decimal precision for Price fields
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Order>()
                .Property(o => o.Total)
                .HasColumnType("decimal(18,2)");

            // String length constraints
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Category>()
                .Property(c => c.CategoryName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(100)
                .IsRequired();

            // Indexes for better performance
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.PublicOrderID)
                .IsUnique();

            modelBuilder.Entity<User>()
    .Property(u => u.ShippingAddressLine1)
    .HasColumnName("ShippingAddressLine1");

        }
    }
}