## FEATURE:

Use event driven architecture to process created orders asynchronously.

### Order creation handling:

When the order is created the OrderCreated event has to be published
- There will be handling of this event which:
    - Update order status: pending → processing
    - Simulate payment processing (5 second delay)
    - Update order status for 50% of cases to completed and publish OrderCompleted event
    - In another 50% of cases do not change the status

### Order expiration handling:

Add recursive job which will run every 60 seconds
- The job find orders with status='processing' older than 10 minutes and update the status to
'expired'
- Publish OrderExpired event

### Notifications handling

- Create new notifications table and add upgrade script/code
- When the OrderCompleted event is published
    - Send email notification (fake/mock - log to console)
    - Save notification to database (audit trail)
- When the OrderExpired event is published
    - Save notification to database (audit trail)

## Expected Flow:
1. User creates order via POST /api/orders
2. Order saved to DB with status='pending'
3. OrderCreated event published
4. OrderProcessor handles event asynchronously:
    - Updates status to 'processing'
    - Simulates payment (5 sec delay)
    - Updates status to 'completed'
5. OrderCompleted event published
6. Notifier handles event:
    - Logs fake email to console
    - Saves notification to DB
7. CRON job runs every 60s:
    - Finds pending orders older than 10 minutes
    - Updates them to 'expired'

## TECHNICAL COMPONENTS:

read architecture.md for detailed view of architecture

### Asynchronous work
- use RabbitMQ for async event flow together with Masstransit
- use event bus for publishing events/commands
    - you can use sagas and consumers to implement whole flow
        - sagas will probably need own database table, so keep that in mind (if you have better solution, you can propose)
- don't forget to update docker file so that everything runs inside of docker locally
    - don't store any secret to files which might be publish to remote, use gitignored files where we already store keys/secrets
- for cron job use basic BackgroundJobs
- events, common code, publishing service, masstransit/rabbitmq initialization should be put to CoderamaOpsAI.Common project
- async code must be idempotent
- you can support retries, let's say 3, after 3 saga (or whole flow) fails and logs


### Other recommendations
- when implementing random processing, keep in mind this has to be testable with unit tests. In tests we have to have deterministic solution, so maybe wrap it in service so that it can be mocked and substituted or create your own solution.
- cron job delay should be in configuration
- for notifications, always include what happened, which order was processed, result, user who started the flow... basically everything to make debugging and issue solving easier


### Testing
- create unit tests for new consumers, async jobs, async flow (saga) - consumed/published commands/events
- copy folder structure of file which is being tested
- test responses, if new entities created in db, Received count of services if any
- use existing DatabaseTestBase for mock db


## DOCUMENTATION:

[Masstransit RabbitMQ documentation](https://masstransit.io/documentation/configuration/transports/rabbitmq)
https://masstransit.io/documentation/configuration
existing codebase

## OTHER CONSIDERATIONS:

check Claude.md also for styles and coding rules