# Celerio Samples

Welcome to the Celerio Samples collection! This project showcases various features and use cases of the Celerio web framework through practical examples. Each sample demonstrates different aspects of building web applications with Celerio, from basic routing to validation, authentication, and more.

## Getting Started

### Running the Samples

1. Navigate to the `Celerio.Samples` directory.
2. Build and run the application:
   ```bash
   dotnet run
   ```
3. The server will start on `http://localhost:5000`.


## Sample Categories

### Basic Endpoints

These endpoints show the fundamentals of routing and response handling in Celerio.

#### GET /
- **Description**: A simple welcome message
- **Example**: `http://localhost:5000/`
- **Response**: Plain text greeting

#### GET /async
- **Description**: Demonstrates asynchronous operations
- **Example**: `http://localhost:5000/async`
- **Response**: A text message after a simulated delay

#### GET /ip
- **Description**: Shows how to access client information
- **Example**: `http://localhost:5000/ip`
- **Response**: Your IP address

### Authorization

Learn how to implement JWT-based authentication in your Celerio applications.

#### GET /auth
- **Description**: Verifies user authentication
- **Authorization**: Requires Bearer token in Authorization header
- **Example**: `http://localhost:5000/auth` with `Authorization: Bearer <token>`
- **Response**: Personalized greeting or authentication error

#### GET /auth/{username}
- **Description**: Generates a JWT token for a user
- **Example**: `http://localhost:5000/auth/Alice`
- **Response**: A new JWT token for the specified username

### Request Body Handling

See how to process JSON payloads in POST requests.

#### POST /article
- **Description**: Accepts and echoes back article data
- **Body**: JSON with `title` and `content` fields
- **Example**:
  ```bash
  curl -X POST http://localhost:5000/article \
    -H "Content-Type: application/json" \
    -d '{"title": "My Article", "content": "This is the content."}'
  ```
- **Response**: Echoed JSON with the submitted data

### Query Parameters

Explore how to handle URL query parameters.

#### GET /search
- **Description**: Performs a search with optional query parameters
- **Parameters**:
  - `query` (optional): Search query string
  - `page` (optional, default 0): Page number
  - `limit` (optional, default 10): Results per page
- **Examples**:
  - `http://localhost:5000/search` (global search)
  - `http://localhost:5000/search?query=celerio&page=1`
- **Response**: JSON with search results

#### GET /sum
- **Description**: Adds two numbers
- **Parameters**:
  - `a` (int): First number
  - `b` (int): Second number
- **Example**: `http://localhost:5000/sum?a=5&b=3`
- **Response**: Text representation of the sum

#### GET /force
- **Description**: Calculates gravitational force (F = m*g)
- **Parameters**:
  - `mass` (float): Mass in kg
  - `g` (optional, default 9.8): Gravitational acceleration
- **Example**: `http://localhost:5000/force?mass=10`
- **Response**: Calculated force as text

### Path Parameters

Discover how to capture dynamic segments from URL paths.

#### GET /path/{text}
- **Description**: Demonstrates simple path parameter capture
- **Example**: `http://localhost:5000/path/hello`
- **Response**: The captured text value

#### GET /path/{text}/subpage
- **Description**: Shows nested path parameters
- **Example**: `http://localhost:5000/path/articles/subpage`
- **Response**: "Subpage of {text}"

#### GET /article/{id}
- **Description**: Retrieves article details by GUID
- **Example**: `http://localhost:5000/article/{guid-id}`
- **Response**: JSON with article data (title, content, id)

#### GET /article/{id}/likes
- **Description**: Gets like count for an article
- **Example**: `http://localhost:5000/article/{guid-id}/likes`
- **Response**: Random like count (simulated)

#### GET /article/{id}/comments/{commentId}
- **Description**: Retrieves a specific comment for an article
- **Example**: `http://localhost:5000/article/{guid-id}/comments/{comment-guid}`
- **Response**: JSON with comment details

### Link Shortener

A practical example of URL shortening service.

#### GET /short
- **Description**: Creates a short code for a given URL
- **Parameter**: `url` (required, must be a valid URL)
- **Example**: `http://localhost:5000/short?url=https://github.com/Oxule/Celerio`
- **Response**: Shortened code

#### GET /short/{code}
- **Description**: Redirects to the original URL using the code
- **Example**: `http://localhost:5000/short/{generated-code}`
- **Response**: HTTP 301 Permanent redirect to original URL

### Validation

Learn about input validation in Celerio using data annotations and custom validators.

#### GET /checkUsername/{username}
- **Description**: Validates username format
- **Constraints**: Length 3-32 characters, alphanumeric only
- **Example**: `http://localhost:5000/checkUsername/AlbertEinstein`
- **Response**: Username if valid, or validation error

#### GET /email
- **Description**: Validates email address format
- **Parameter**: `email` (required, valid email format)
- **Example**: `http://localhost:5000/email?email=user@example.com`
- **Response**: Accepted email or validation error

#### GET /url
- **Description**: Validates and redirects to a URL
- **Parameter**: `url` (required, valid URL format)
- **Example**: `http://localhost:5000/url?url=https://celer.io`
- **Response**: HTTP 302 redirect to the URL

#### POST /validators/validatable
- **Description**: Demonstrates custom validation with IValidatable
- **Body**: JSON with `name` and `age` fields
- **Validation**:
  - Name cannot be empty
  - Age must be >= 0
  - Age must be >= 18 (adult only)
- **Example**:
  ```bash
  curl -X POST http://localhost:5000/validators/validatable \
    -H "Content-Type: application/json" \
    -d '{"name": "Alice", "age": 25}'
  ```
- **Response**: Processed person data or validation error

## Contributing

If you'd like to add more sample endpoints or improve existing ones:
1. Create a new static class in the project
2. Implement methods with appropriate [Get], [Post], etc. attributes
3. Follow the existing patterns for consistency
4. Test your endpoints using curl or a browser