.PHONY: start stop restart status

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
