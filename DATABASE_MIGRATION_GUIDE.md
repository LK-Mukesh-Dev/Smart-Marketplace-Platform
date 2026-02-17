# Database Migration Guide - UserService

## Prerequisites

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Or update if already installed
dotnet tool update --global dotnet-ef
```

---

## Creating the First Migration

### Step 1: Navigate to API Project
```bash
cd src/Services/UserService/UserService.API
```

### Step 2: Create Initial Migration
```bash
dotnet ef migrations add InitialCreate --project ../UserService.Infrastructure
```

This will create:
- `Migrations/XXXXXX_InitialCreate.cs` - Migration file
- `Migrations/UserDbContextModelSnapshot.cs` - Model snapshot

### Step 3: Review Generated SQL (Optional)
```bash
dotnet ef migrations script --project ../UserService.Infrastructure
```

### Step 4: Apply Migration to Database
```bash
dotnet ef database update
```

---

## Verify Database Creation

### Using MySQL Command Line
```bash
# Connect to MySQL
mysql -h localhost -P 3306 -u root -p

# Enter password: root

# Show databases
SHOW DATABASES;

# Use UserService database
USE UserServiceDb;

# Show tables
SHOW TABLES;

# Describe Users table
DESCRIBE Users;

# View migrations history
SELECT * FROM __EFMigrationsHistory;
```

### Expected Users Table Structure
```sql
+-------------+--------------+------+-----+---------+-------+
| Field       | Type         | Null | Key | Default | Extra |
+-------------+--------------+------+-----+---------+-------+
| Id          | char(36)     | NO   | PRI | NULL    |       |
| Email       | varchar(255) | NO   | UNI | NULL    |       |
| PasswordHash| longtext     | NO   |     | NULL    |       |
| FirstName   | varchar(100) | NO   |     | NULL    |       |
| LastName    | varchar(100) | NO   |     | NULL    |       |
| PhoneNumber | varchar(20)  | YES  |     | NULL    |       |
| Role        | int          | NO   |     | NULL    |       |
| IsActive    | tinyint(1)   | NO   |     | NULL    |       |
| CreatedAt   | datetime(6)  | NO   |     | NULL    |       |
| UpdatedAt   | datetime(6)  | YES  |     | NULL    |       |
| LastLoginAt | datetime(6)  | YES  |     | NULL    |       |
+-------------+--------------+------+-----+---------+-------+
```

---

## Adding New Migrations

### Scenario: Add new property to User entity

1. **Update Domain Entity** (`UserService.Domain/Entities/User.cs`):
```csharp
public class User
{
    // ...existing properties...
    public string Address { get; set; } = string.Empty; // NEW
}
```

2. **Create Migration**:
```bash
cd src/Services/UserService/UserService.API
dotnet ef migrations add AddAddressToUser --project ../UserService.Infrastructure
```

3. **Apply Migration**:
```bash
dotnet ef database update
```

---

## Common Migration Commands

### List All Migrations
```bash
dotnet ef migrations list --project ../UserService.Infrastructure
```

### Remove Last Migration (not applied)
```bash
dotnet ef migrations remove --project ../UserService.Infrastructure
```

### Rollback to Previous Migration
```bash
# Get migration name from list
dotnet ef migrations list --project ../UserService.Infrastructure

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project ../UserService.Infrastructure
```

### Drop Database
```bash
dotnet ef database drop --force --project ../UserService.Infrastructure
```

### Generate SQL Script
```bash
# All migrations
dotnet ef migrations script --project ../UserService.Infrastructure

# From specific migration to latest
dotnet ef migrations script FromMigration --project ../UserService.Infrastructure

# Between two migrations
dotnet ef migrations script FromMigration ToMigration --project ../UserService.Infrastructure
```

---

## Seeding Initial Data

### Option 1: Add to DbContext (Recommended)

Update `UserService.Infrastructure/DbContext/UserDbContext.cs`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ...existing configuration...

    // Seed Admin User
    var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var passwordHasher = new PasswordHasher();
    
    modelBuilder.Entity<User>().HasData(
        new User
        {
            Id = adminId,
            Email = "admin@marketplace.com",
            PasswordHash = passwordHasher.HashPassword("Admin123!"),
            FirstName = "System",
            LastName = "Administrator",
            PhoneNumber = "+1234567890",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }
    );
}
```

Then create migration:
```bash
dotnet ef migrations add SeedAdminUser --project ../UserService.Infrastructure
dotnet ef database update
```

### Option 2: Manual SQL Insert

```sql
INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, PhoneNumber, Role, IsActive, CreatedAt)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'admin@marketplace.com',
    '$2a$11$...',  -- BCrypt hash of 'Admin123!'
    'System',
    'Administrator',
    '+1234567890',
    1,  -- Admin role
    1,  -- IsActive
    UTC_TIMESTAMP()
);
```

---

## Connection String Configuration

### Development (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=UserServiceDb;User=root;Password=root;"
  }
}
```

### Docker (docker-compose.yml)
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Server=mysql;Database=UserServiceDb;User=root;Password=root;
```

### Production (Environment Variables)
```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=UserServiceDb;User=prod_user;Password=secure_pass;"
```

---

## Troubleshooting

### Error: "Unable to create migrations"

**Solution**: Make sure you're in the API project directory
```bash
cd src/Services/UserService/UserService.API
dotnet ef migrations add MigrationName --project ../UserService.Infrastructure
```

### Error: "Connection refused"

**Solution**: Verify MySQL is running
```bash
docker ps | findstr mysql
# If not running:
docker start mysql
# Or start with docker-compose:
docker-compose up -d mysql
```

### Error: "Access denied for user"

**Solution**: Check credentials in appsettings.json and MySQL
```bash
mysql -h localhost -P 3306 -u root -p
# Try password: root
```

### Error: "Duplicate column name"

**Solution**: Column already exists, remove migration
```bash
dotnet ef migrations remove --project ../UserService.Infrastructure
```

### Error: "Build failed"

**Solution**: Build the solution first
```bash
dotnet build
# Then run migration
dotnet ef migrations add MigrationName --project ../UserService.Infrastructure
```

---

## Migration Best Practices

### 1. Always Review Generated Migrations
```bash
# View migration file before applying
code src/Services/UserService/UserService.Infrastructure/Migrations/XXXX_MigrationName.cs
```

### 2. Test Migrations in Development First
```bash
# Create test database
dotnet ef database update --project ../UserService.Infrastructure

# Test rollback
dotnet ef database update PreviousMigration --project ../UserService.Infrastructure

# Test forward again
dotnet ef database update --project ../UserService.Infrastructure
```

### 3. Keep Migrations Small
- One migration per logical change
- Don't combine unrelated changes
- Use descriptive names

### 4. Never Edit Applied Migrations
- Once applied to production, never modify
- Create new migration to fix issues
- Use `dotnet ef migrations add FixIssueName`

### 5. Backup Before Production Migrations
```bash
# MySQL backup
mysqldump -u root -p UserServiceDb > backup_$(date +%Y%m%d_%H%M%S).sql

# Or use Docker
docker exec mysql mysqldump -u root -proot UserServiceDb > backup.sql
```

---

## Production Deployment Checklist

- [ ] Test migrations in development
- [ ] Test migrations in staging
- [ ] Create database backup
- [ ] Generate SQL script for review
- [ ] Schedule maintenance window
- [ ] Apply migration to production
- [ ] Verify application startup
- [ ] Test critical endpoints
- [ ] Monitor for errors
- [ ] Keep rollback plan ready

---

## Useful Scripts

### Create Migration Script for Production
```bash
# Generate script from last production migration to latest
dotnet ef migrations script LastProductionMigration --output migration.sql --project ../UserService.Infrastructure --idempotent
```

The `--idempotent` flag makes the script safe to run multiple times.

### Reset Database (Development Only)
```bash
cd src/Services/UserService/UserService.API

# Drop database
dotnet ef database drop --force --project ../UserService.Infrastructure

# Recreate and apply all migrations
dotnet ef database update --project ../UserService.Infrastructure
```

### Quick Reset Script (PowerShell)
```powershell
# save as reset-db.ps1
cd src/Services/UserService/UserService.API
dotnet ef database drop --force --project ../UserService.Infrastructure
dotnet ef database update --project ../UserService.Infrastructure
Write-Host "Database reset complete!" -ForegroundColor Green
```

---

## References

- [EF Core Migrations Docs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core CLI Reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [Pomelo MySQL Provider](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)

---

**Quick Reference Card**:
```bash
# Create migration
dotnet ef migrations add MigrationName --project ../UserService.Infrastructure

# Apply migration
dotnet ef database update

# List migrations
dotnet ef migrations list --project ../UserService.Infrastructure

# Remove last migration
dotnet ef migrations remove --project ../UserService.Infrastructure

# Drop database
dotnet ef database drop --force --project ../UserService.Infrastructure

# Generate SQL
dotnet ef migrations script --project ../UserService.Infrastructure
```
