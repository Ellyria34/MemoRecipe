# memorecipe-api
API REST .NET 10 + PostgreSQL + Auth + IA

MemoRecipe API is the backend powering **MemoRecipe**, an application designed to:  
- manage a personal cookbook  
- structure recipes through a guided form  
- import recipes from photos/scans (AI OCR)  
- handle secure user accounts  
- sync data across devices (PWA / mobile / cloud)


## Technologies
 
|         Technologie         |                 Usage                    |
|-----------------------------|------------------------------------------|
|      **.NET 10 (LTS)**      |               Core REST API              | 
|         **C# 14**           |         Modern language features         |
|       **EF Core 10**        |             ORM for PostgreSQL           |
|      **PostgreSQL 16**      |               Main database              |
|          **pgAdmin 4**      |           PostgreSQL Interface           |
| **Docker / Docker Compose** |           evelopment environment         |
|    **JWT Authentication**   |           User auth and security         |
|      **Azure Functions**    |           OCR and recipe parsing         |
|         **Swagger**         |              API documentation           |
 

## Project Architecture

The API follows **Clean Architecture**, providing high modularity, testability, and separation of concerns.

```
memorecipe-api/
├── src/
│ ├── MemoRecipe.Api → API layer (.NET 10)
│ ├── MemoRecipe.Application → Business logic + Interfaces
│ ├── MemoRecipe.Domain → Entities + ValueObjects + Enums
│ └── MemoRecipe.Infrastructure → PostgreSQL + EF Core + Repositories
|
├── docker-compose.yml → PostgreSQL + PGAdmin
└── README.md
```

## Clone the Repository
```
git clone https://github.com/<your-username>/memorecipe-api.git
cd memorecipe-api
```

## Restore dependencies
```
dotnet restore
```
## Restore dependencies
```
dotnet run --project ./src/MemoRecipe.Api
```

API will start on: http://localhost:5131
Swagger UI: http://localhost:5131/swagger









## Database Model (PostgreSQL)
Main entities:

**Users**
    - Authentication data
    - Role (User / Admin)
    - Related recipes, favorites, comments

**Recipes**
  - Title, description, difficulty, timings
  - Ingredients
  - Steps (ordered)
  - Images
  - Categories (many-to-many)
  - Comments
  - Favorites
  - OCR extraction (optional)
  - Source metadata (optional)
  - Nutrition potential (planned)

**Ingredients**
  - Per-recipe ingredients
  - Linked optionally to a FoodProduct (barcode DB)

**IngredientNutrition**
  - Nutritional values per 100g: Calories, Proteins, Carbs, Fats, Sugars, etc.
  - Allergens (JSONB)

**FoodProduct**
  - Future barcode item database

**OCRExtraction**
  - Raw OCR text
  - AI-ready structured JSON (JSONB)

**RecipeSource**
 - URL, book title/page, metadata (JSONB)

**Categories**
  - Many-to-many relationship with recipes

**Favorites**
  - Many-to-many between Users & Recipes

**Comments**
  - User + recipe + content + rating








## Run the project locally

### Requirements

- .NET 10 SDK  
- Docker Desktop  
- PostgreSQL (via Docker)  
- VS Code or Visual Studio  
- Git  

Check your .NET installation: dotnet --version

### Start PostgreSQL using Docker
The repository includes a docker-compose.yml file.

Start PostgreSQL + pgAdmin: docker compose up -d
Stop containers: docker compose down

Default connection:
Host: localhost
Port: 5432
User: memorecipe
Password: memorecipe
Database: memorecipe

### API Configuration
Create a local config file: appsettings.Development.json
```
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=memorecipe;Username=memorecipe;Password=memorecipe"
  },
  "Jwt": {
    "Key": "dev-secret-key-change",
    "Issuer": "MemoRecipe",
    "Audience": "MemoRecipeUsers",
    "ExpirationMinutes": 60
  }
}
```
### Add migration:
dotnet ef migrations add MigrationName --project src/MemoRecipe.Infrastructure --startup-project src/MemoRecipe.Api

### Update database:
dotnet ef database update --project src/MemoRecipe.Infrastructure --startup-project src/MemoRecipe.Api

### Start the API
dotnet run --project src/MemoRecipe.Api

The API will be available at:
https://localhost:7001
http://localhost:5000

Swagger UI (interactive API docs): https://localhost:7001/swagger




### Authentication
The API uses JWT Bearer Authentication.
Main endpoints:

Method	Endpoint	Description
POST	/auth/register	Create new user
POST	/auth/login	Generate tokens
POST	/auth/refresh	Refresh JWT

### AI (OCR + Recipe Parsing)

AI services are handled by Azure Functions:

Extracting raw text from images (OCR)

Structuring ingredients

Splitting cooking steps

Suggesting corrections

Architecture: API → Azure Function → Save result in JSONB → Return structured recipe.

### Testing
Run all tests: dotnet test

### Deployment
This project supports deployment to:

**API**
- Azure App Service
- Azure Container Apps
- Docker + VPS

**Database**
- Azure PostgreSQL Flexible Server
- Managed PostgreSQL on VPS

**AI**
- Azure Functions Consumption Plan

Optional CI/CD pipelines via GitHub Actions.

## Author
Ellyria34
MemoRecipe Project — 2025

## License
This project is currently private and not licensed for public distribution.
