.PHONY: start stop restart status terraform-build terraform-init terraform-plan terraform-apply terraform-destroy setup-infrastructure

start:
	docker-compose up -d
	@echo "LocalStack started"

stop:
	docker-compose down
	@echo "LocalStack stopped"

restart: stop start
	@echo "LocalStack restarted"

status:
	docker-compose ps

terraform-build:
	docker build -f docker/Dockerfile.terraform -t terraform-local .

terraform-init: terraform-build
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local init

terraform-plan: terraform-init
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local plan

terraform-apply: terraform-init
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local apply -auto-approve

terraform-destroy:
	docker run --rm --network host -v $(PWD)/terraform:/terraform terraform-local destroy -auto-approve

setup-infrastructure: start terraform-apply
	@echo "Infrastructure setup complete"
