variable "secrets" {
  description = "Map of secrets to create"
  type = map(object({
    name        = string
    description = string
    secret_data = map(string)
  }))
  default = {
    app_secrets = {
      name        = "dotnet-app-secrets"
      description = "Application secrets for .NET app"
      secret_data = {
        database_url    = "postgresql://localhost:5432/mydb"
        api_key        = "your-api-key-here"
        jwt_secret     = "your-jwt-secret"
        redis_url      = "redis://localhost:6379"
      }
    }
  }
}
