output "secret_arns" {
  description = "ARNs of created secrets"
  value = {
    for k, v in aws_secretsmanager_secret.app_secrets : k => v.arn
  }
}

output "secret_names" {
  description = "Names of created secrets"
  value = {
    for k, v in aws_secretsmanager_secret.app_secrets : k => v.name
  }
}
