# Quick Start Guide - Smart Marketplace Platform

## ? What's Already Implemented

### Completed Services
1. **API Gateway (Ocelot)** - Port 5000
2. **User Service** - Port 5001
3. **Shared Libraries** (EventBus, Common)
4. **Docker Infrastructure**

---

## ?? Getting Started

### Option 1: Docker Compose (Recommended)

```bash
# Clone the repository
cd C:\Users\hp\OneDrive\Desktop\Projects\Smart-Marketplace-Platform

# Start all services (MySQL, Kafka, Redis, Gateway, UserService)
docker-compose up --build

# Access the services
# Gateway: http://localhost:5000
# UserService: http://localhost:5001
# Swagger: http://localhost:5001/swagger
```

### Option 2: Local Development

#### Step 1: Start Infrastructure Services
```bash
# MySQL
docker run -d -p 3306:3306 --name mysql \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=UserServiceDb \
  mysql:8.0

# Kafka & Zookeeper
docker run -d -p 2181:2181 --name zookeeper \
  -e ZOOKEEPER_CLIENT_PORT=2181 \
  confluentinc/cp-zookeeper:latest

docker run -d -p 9092:9092 --name kafka \
  --link zookeeper \
  -e KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 \
  -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092 \
  -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
  confluentinc/cp-kafka:latest

# Redis
docker run -d -p 6379:6379 --name redis redis:latest
```

#### Step 2: Run Database Migrations
```bash
cd src/Services/UserService/UserService.API

# Install EF Core tools if not installed
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add InitialCreate --project ../UserService.Infrastructure

# Apply migration
dotnet ef database update
```

#### Step 3: Run Services
```bash
# Terminal 1 - User Service
cd src/Services/UserService/UserService.API
dotnet run

# Terminal 2 - Gateway
cd src/Gateway/Gateway.API
dotnet run
```

---

## ?? Testing the API

### 1. Register a New User

**Request**:
```bash
curl -X POST http://localhost:5001/api/user/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890",
    "role": 0
  }'
```

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "role": 0,
  "isActive": true,
  "createdAt": "2025-01-28T10:00:00Z",
  "lastLoginAt": null
}
```

### 2. Login

**Request**:
```bash
curl -X POST http://localhost:5001/api/user/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "Password123!"
  }'
```

**Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890",
    "role": 0,
    "isActive": true,
    "createdAt": "2025-01-28T10:00:00Z",
    "lastLoginAt": "2025-01-28T10:05:00Z"
  }
}
```

### 3. Get Current User Profile (Protected)

**Request**:
```bash
curl -X GET http://localhost:5001/api/user/me \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

**Response**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "role": 0,
  "isActive": true,
  "createdAt": "2025-01-28T10:00:00Z",
  "lastLoginAt": "2025-01-28T10:05:00Z"
}
```

### 4. Get User by ID (Protected)

**Request**:
```bash
curl -X GET http://localhost:5001/api/user/profile/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

---

## ?? Using Swagger UI

1. Navigate to: http://localhost:5001/swagger
2. Try out the endpoints:
   - Click on an endpoint
   - Click "Try it out"
   - Fill in the parameters
   - Click "Execute"

For protected endpoints:
1. Click the "Authorize" button (??)
2. Enter: `Bearer YOUR_JWT_TOKEN`
3. Click "Authorize"

---

## ?? Database Access

### Using MySQL Workbench or CLI

```bash
# Connect to MySQL
mysql -h localhost -P 3306 -u root -p

# Enter password: root

# Use the database
USE UserServiceDb;

# View users
SELECT * FROM Users;

# View structure
DESCRIBE Users;
```

---

## ?? Docker Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f userservice

# Restart a service
docker-compose restart userservice

# Rebuild and restart
docker-compose up --build

# Remove all containers and volumes
docker-compose down -v
```

---

## ??? Development Commands

### Build Solution
```bash
dotnet build
```

### Run Tests (when created)
```bash
dotnet test
```

### Restore Packages
```bash
dotnet restore
```

### Clean Build
```bash
dotnet clean
dotnet build
```

### Add New Migration
```bash
cd src/Services/UserService/UserService.API
dotnet ef migrations add MigrationName --project ../UserService.Infrastructure
```

### Rollback Migration
```bash
dotnet ef database update PreviousMigrationName
```

### Drop Database
```bash
dotnet ef database drop --force
```

---

## ?? Project Structure

```
Smart-Marketplace-Platform/
??? src/
?   ??? Gateway/
?   ?   ??? Gateway.API/          # API Gateway (Port 5000)
?   ??? Services/
?   ?   ??? UserService/          # User Service (Port 5001)
?   ?       ??? UserService.API/
?   ?       ??? UserService.Application/
?   ?       ??? UserService.Domain/
?   ?       ??? UserService.Infrastructure/
?   ??? Shared/
?       ??? EventBus/             # Kafka integration
?       ??? Common/               # Shared utilities
??? docker-compose.yml            # Infrastructure setup
```

---

## ?? Configuration

### UserService - appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=UserServiceDb;User=root;Password=root;"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!@#$%",
    "Issuer": "UserService",
    "Audience": "SmartMarketplace",
    "ExpirationInHours": "24"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

### Gateway - ocelot.json
- Routes configured for UserService
- Rate limiting: 100 requests per minute
- JWT authentication ready

---

## ?? User Roles

- **0 = User** (Default)
- **1 = Admin**

Example register as Admin:
```json
{
  "email": "admin@example.com",
  "password": "AdminPass123!",
  "firstName": "Admin",
  "lastName": "User",
  "phoneNumber": "+1234567890",
  "role": 1
}
```

---

## ?? Security Notes

1. **Password Requirements**:
   - Minimum 8 characters
   - Hashed with BCrypt

2. **JWT Token**:
   - Valid for 24 hours
   - Contains: UserId, Email, Role
   - Must be sent in Authorization header: `Bearer {token}`

3. **Email Validation**:
   - Must be valid email format
   - Converted to lowercase
   - Must be unique

---

## ?? Common Issues & Solutions

### Issue: Port already in use
```bash
# Find process using the port
netstat -ano | findstr :5001

# Kill the process (Windows)
taskkill /PID <PID> /F

# Or change the port in launchSettings.json
```

### Issue: Database connection failed
```bash
# Verify MySQL is running
docker ps | findstr mysql

# Restart MySQL
docker restart mysql

# Check connection string in appsettings.json
```

### Issue: Kafka connection failed
```bash
# Verify Kafka is running
docker ps | findstr kafka

# Restart Kafka
docker restart kafka zookeeper
```

### Issue: Migration error
```bash
# Drop and recreate database
dotnet ef database drop --force
dotnet ef database update
```

---

## ?? API Documentation

Once running, access:
- **Swagger UI**: http://localhost:5001/swagger
- **OpenAPI JSON**: http://localhost:5001/swagger/v1/swagger.json

---

## ?? Success Indicators

? Build successful
? Gateway running on port 5000
? UserService running on port 5001
? MySQL accessible on port 3306
? Kafka running on port 9092
? Swagger UI accessible
? User registration working
? User login returning JWT
? Protected endpoints requiring auth

---

## ?? Next Steps

1. **Test all endpoints** using Swagger or Postman
2. **Create Product Service** following UserService pattern
3. **Add more services** (Order, Inventory, Payment)
4. **Implement frontend** (Angular + PrimeNG)
5. **Add unit tests** for handlers
6. **Set up CI/CD** pipeline

---

## ?? Learning Resources

- **Clean Architecture**: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- **Ocelot Gateway**: https://ocelot.readthedocs.io/
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **JWT Authentication**: https://jwt.io/introduction
- **Apache Kafka**: https://kafka.apache.org/documentation/

---

**Status**: ? All systems operational and ready for development!
