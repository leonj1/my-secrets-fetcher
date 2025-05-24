resource "aws_secretsmanager_secret" "app_secrets" {
  for_each    = var.secrets
  name        = each.value.name
  description = each.value.description
}

resource "aws_secretsmanager_secret_version" "app_secrets" {
  for_each      = var.secrets
  secret_id     = aws_secretsmanager_secret.app_secrets[each.key].id
  secret_string = jsonencode(each.value.secret_data)
}
