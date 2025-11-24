# Test Users

The database is seeded with the following test users for development and testing:

## User 1: Admin User
- **Email:** `admin@example.com`
- **Password:** `Admin123!`
- **Name:** Admin User

## User 2: Test User
- **Email:** `test@example.com`
- **Password:** `Test123!`
- **Name:** Test User

## Usage

Use these credentials to test the `/api/auth/login` endpoint in Swagger or with any HTTP client.

Example request body:
```json
{
  "email": "admin@example.com",
  "password": "Admin123!"
}
```

The response will include a JWT token valid for 10 minutes.
