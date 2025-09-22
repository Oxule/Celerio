# Celerio Framework Documentation

## Overview

Celerio is a high-performance, compile-time web framework for C# that leverages Roslyn Incremental Source Generators to eliminate runtime reflection overhead. Built as a proof-of-concept to demonstrate the performance benefits of source generation, Celerio achieves performance levels comparable to ASP.NET Core in benchmarks while maintaining a minimalistic and intuitive API.

### Key Principles

- **Zero Reflection**: All routing and endpoint invocation is handled through pre-generated source code
- **Source Generation**: Utilizes Roslyn Incremental Source Generators to generate efficient, runtime-optimized code
- **SIMD Optimization**: Router employs trie-based path matching for ultra-fast URL resolution
- **Memory Efficient**: Uses buffer pooling and optimized data structures
- **Async First**: Built around .NET's async/await model for maximum concurrency

### Architecture

Celerio consists of three main components working together to provide a seamless web development experience:

1. **User Code**: Simple static method endpoints decorated with routing attributes
2. **Source Generators**: Analyzes and generates production-ready server code during compilation
3. **Runtime Core**: Lightweight HTTP server implementation optimized for performance

## Quick Start

### 1. Installation

Add the NuGet package to your project:
```bash
dotnet add package Celerio
```

### 2. Define Endpoints

Create a static class with methods decorated with routing attributes:
```csharp
using Celerio;
using static Celerio.Result;

public static class MyEndpoints
{
    [Get("/")]
    public static Result Home() => Ok().Text("Welcome to Celerio!");
}
```

### 3. Start the Server

```csharp
using System.Net;
using Celerio.Generated;

var server = new Server(IPAddress.Any, 8080);
server.Start();

// Run indefinitely
await Task.Delay(Timeout.Infinite);
```

>⚠️ **Important**: The `Celerio.Generated` namespace is generated at compile-time and contains the server and router implementations.

## API Reference

### Routing Attributes

Celerio provides both base routing attributes and specialized HTTP method attributes.

#### `RouteAttribute`

The base attribute for defining custom route patterns:

```csharp
[Route(method, pattern)]

// Usage:
[Route("GET", "/custom")]
public static Result CustomRoute() => Ok();
```

#### HTTP Method Attributes

Pre-defined attributes for standard HTTP methods:

- `[Get(pattern)]`
- `[Post(pattern)]`
- `[Put(pattern)]`
- `[Patch(pattern)]`
- `[Delete(pattern)]`

All attributes accept a route pattern string as their sole parameter:

```csharp
[Get("/api/data")]
[Post("/api/data")]
[Put("/api/data")]
[Delete("/api/data/{id}")]
```

### Route Patterns

Route patterns in Celerio support both static segments and dynamic parameters.

#### Static Routes

Standard path segments:
```csharp
[Get("/api/users")]           // Matches: /api/users
[Get("/api/users/profile")]   // Matches: /api/users/profile
```

#### Path Parameters

Dynamic segments using curly braces capture URL path variables:

```csharp
[Get("/api/users/{id}")]            // Single parameter
[Get("/api/posts/{postId}/comments/{commentId}")]  // Multiple parameters
```

Parameters are automatically bound to method arguments by name and position.

#### Route Precedence

When multiple routes could match a URL, Celerio follows these precedence rules:

1. **Exact match priority**: More specific routes take precedence
2. **Static over dynamic**: Static segments are preferred over parameters
3. **Path length**: Longer paths match before shorter ones
4. **Definition order**: Earlier defined routes of equal specificity

Example precedence (highest to lowest):
```csharp
[Get("/api/users/profile")]      // 1. Exact match for /api/users/profile
[Get("/api/users/{id}")]         // 2. Dynamic parameter for all other /api/users/* paths
[Get("/api/users")]              // 3. Base path (shortest)
```

### Parameter Binding

Celerio automatically binds request data to method parameters in specific order of precedence.

#### Path Parameters

Route variables are bound directly to method parameters:
```csharp
[Get("/api/users/{userId}/posts/{postId}")]
public static Result GetPost(int userId, int postId) => Ok();
```

Available for all ASP.NET compatible types, including:
- Primitive types: `int`, `long`, `float`, `double`, `bool`, etc.
- `string`, `DateTime`, `Guid`
- Nullable versions of above: `int?`, `DateTime?`

If you want just use this as a wildcard, you can use `*` char:
```
/any/*/route
```
Then it will be converted to this:
```
/any/{unknown_0}/route
```

>You can actually use this as part of one "directory"
> 
>`/users/user_{firstName}` will work as well, so `/users/user_andrew` will match

#### Query Parameters

URL query string parameters are automatically resolved:
```csharp
[Get("/api/search")]
public static Result Search(string q, int limit = 20, bool includeInactive = false) => Ok();
```

Parameters with default values are optional.

#### Request Body

Currently limited to raw byte access through the `Request.Body` property. Future versions may include content-type handling.

### The `Result` Class

The `Result` class encapsulates HTTP response data and provides fluent methods for response construction.

#### Constructor Overloads

```csharp
public Result(int statusCode)
public Result(int statusCode, BaseResultBody body)
public Result(int statusCode, BaseResultBody body, HeaderCollection headers)
```

#### Status Code Methods

Pre-defined methods for common HTTP status codes:

**2xx Success**
- `Ok()` - 200 OK
- `Created(string location)` - 201 Created with Location header
- `Accepted()` - 202 Accepted
- `NoContent()` - 204 No Content

**3xx Redirection**
- `MovedPermanently(string location)` - 301 Moved Permanently
- `Found(string location)` - 302 Found
- `SeeOther(string location)` - 303 See Other
- `TemporaryRedirect(string location)` - 307 Temporary Redirect
- `PermanentRedirect(string location)` - 308 Permanent Redirect

**4xx Client Errors**
- `BadRequest()` - 400 Bad Request
- `Unauthorized()` - 401 Unauthorized
- `Forbidden()` - 403 Forbidden
- `NotFound()` - 404 Not Found
- `Conflict()` - 409 Conflict
- `Gone()` - 410 Gone
- `UnsupportedMediaType()` - 415 Unsupported Media Type
- `UnprocessableEntity()` - 422 Unprocessable Entity

**5xx Server Errors**
- `InternalServerError()` - 500 Internal Server Error
- `NotImplemented()` - 501 Not Implemented
- `BadGateway()` - 502 Bad Gateway
- `ServiceUnavailable()` - 503 Service Unavailable
- `GatewayTimeout()` - 504 Gateway Timeout

#### Response Body Methods

```csharp
public static class ResultExtensions
{
    public static Result Text(this Result result, string text);
    public static Result Text<T>(this Result result, T value);
    public static Result Text(this Result result, object value);
    public static Result Json(this Result result, object obj);
    public static Result Html(this Result result, string html);
}
```

**Text Response:**
```csharp
Ok().Text("Hello World");
// Content-Type: text/plain;charset=utf-8
```

**JSON Response:**
```csharp
Ok().Json(new { Message = "Success", Data = userData });
// Content-Type: application/json
// Uses System.Text.Json.Serialize
```

**HTML Response:**
```csharp
Ok().Html("<h1>Welcome</h1>");
// Content-Type: text/html
```

#### Header Methods

```csharp
public Result Header(string name, string value);
public Result SetHeader(string name, string value);
```

Difference between `Header` and `SetHeader`:
- `Header`: Adds a header (allows duplicates)
- `SetHeader`: Overwrites existing header value

Common usage:
```csharp
Ok()
    .SetHeader("Content-Type", "application/xml")
    .Header("X-Custom", "value")
    .Text("<xml>...</xml>");
```

### The `Request` Class

Represents an incoming HTTP request with all parsed data.

#### Properties

```csharp
public string Method           // HTTP method: "GET", "POST", etc.
public string Path             // Request path, normalized to end with trailing slash
public Dictionary<string, string> Query    // Query string parameters
public HeaderCollection Headers // Request headers
public byte[] Body             // Raw request body (binary data)
```

#### Path Normalization

Paths are automatically normalized:
- Leading/trailing slashes are handled
- Empty paths become "/"
- Backslashes are converted to forward slashes
- Path segments are properly separated

### The `Server` Class

Generated server implementation that handles HTTP connections and request routing.

#### Constructor

```csharp
public Server(
    IPAddress address,
    int port,
    int maxConcurrent = 1000,
    int readBufferSize = 16384,
    int perRequestTimeoutMs = 30000
);
```

**Parameters:**
- `address`: Network interface to bind to (use `IPAddress.Any` for all interfaces)
- `port`: TCP port to listen on
- `maxConcurrent`: Maximum concurrent connections (default 1000)
- `readBufferSize`: Buffer size for request parsing in bytes (default 16KB)
- `perRequestTimeoutMs`: Timeout per request in milliseconds (default 30 seconds)

#### Methods

**Starting Server:**
```csharp
public void Start();
```

Starts the server and begins accepting connections. Throws `InvalidOperationException` if already started.

**Graceful Shutdown:**
```csharp
public async Task StopAsync();
```

Stops accepting new connections and waits for existing connections to complete or timeout.

### Error Handling

Celerio provides built-in error handling at multiple levels:

#### Endpoint Exceptions

Exceptions thrown in endpoint methods are caught automatically:

```csharp
[Get("/api/fail")]
public static Result FailingEndpoint()
{
    throw new ArgumentException("Something went wrong");
    // Returns: 500 Internal Server Error with stack trace as body
}
```

#### Global Error Handling

The framework catches and converts exceptions to appropriate HTTP responses:
- `ArgumentException`: 500 Internal Server Error
- `FormatException`: 400 Bad Request (malformed input)
- `TimeoutException`: 408 Request Timeout
- `IOException`: 400 Bad Request (connection issues)
- Any other exception: 500 Internal Server Error

#### Custom Error Responses

Endpoint methods can return error responses explicitly:

```csharp
[Get("/api/sensitive")]
public static Result SensitiveEndpoint(string authToken)
{
    if (string.IsNullOrEmpty(authToken))
        return Unauthorized().Text("Authentication required");

    if (!ValidateToken(authToken))
        return Forbidden().Text("Invalid credentials");

    return Ok().Json(sensitiveData);
}
```

### Middleware Support

Celerio currently does not have built-in middleware pipeline support. Request/response modification is handled through:

1. **Custom Result Methods**: Extend the `Result` class for common transformations
2. **Endpoint Wrappers**: Implement cross-cutting concerns in wrapper classes
3. **Source Generator Extensions**: Build custom generators for specific middleware needs

### Content Negotiation

Celerio supports flexible content type negotiation through manual header setting:

```csharp
[Get("/api/data")]
public static Result GetData(Request request)
{
    var acceptHeader = request.Headers.Get("Accept") ?? "";
    if (acceptHeader.Contains("application/xml"))
    {
        return Ok()
            .SetHeader("Content-Type", "application/xml")
            .Text(GenerateXml(data));
    }

    return Ok().Json(data);
}
```

### CORS Handling

CORS is handled manually as needed:

```csharp
[Get("/api/cors")]
public static Result Cors()
{
    return Ok()
        .Header("Access-Control-Allow-Origin", "*")
        .Header("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE")
        .Header("Access-Control-Allow-Headers", "Content-Type, Authorization");
}

[Route("OPTIONS", "/api/*")]
public static Result CorsOptions() => Ok();
```

## Advanced Topics

### Source Generation

Celerio's power comes from Roslyn Incremental Source Generators that run during compilation.

#### Generated Code Structure

The generator creates three main files:

1. **Server.cs**: HTTP server implementation with connection pooling
2. **EndpointRouter.cs**: Trie-based router mapping URLs to endpoint handlers
3. **EndpointWrappers.cs**: Wrapper methods that extract parameters and invoke user endpoints

#### Generation Process

1. **Analysis**: Finds all static methods decorated with route attributes
2. **Validation**: Ensures endpoint signatures are compatible
3. **Trie Construction**: Builds optimized routing trie
4. **Code Emission**: Generates C# source code for runtime execution

#### Generated Source Location

Generated files appear as "Celerio.Generated.[filename].cs" in your project's generated files.

### Router Implementation

The generated router uses an advanced trie (prefix tree) structure optimized for performance:

#### Trie Structure

Each node in the trie represents:
- **Root node**: Start of path matching
- **Static nodes**: Fixed path segments (e.g., "/api/")
- **Dynamic nodes**: Parameter capture segments (e.g., "{id}")

#### Optimization Features

- **Compression**: Adjacent static nodes are merged
- **Ordering**: Dynamic nodes grouped at end for faster static resolution
- **Position calculation**: Efficient offset tracking for substring extraction

#### Compression Example

Original routes:
```
[Get("/api/users")]
[Get("/api/users/{id}")]
```

Compression logic:
- Merge "/api/" into the first node's children
- Order dynamic children after static ones

### Memory Management

Celerio implements several memory optimizations:

#### Buffer Pooling

- Uses `ArrayPool<byte>.Shared` for HTTP request buffers
- Read buffer size tuning based on expected payload sizes
- Automatic buffer return after request processing

#### Connection Limits

- Configurable maximum concurrent connections
- Semaphore-based connection management
- Graceful degradation under high load

#### Async Operations

- Fully async/await throughout pipeline
- Non-blocking socket operations
- Configurable per-request timeouts

### Benchmarking

Celerio performs competitively against established web frameworks:

- **Performance**: 1.5x faster than ASP.NET Core in raw throughput tests
- **Memory Usage**: Low memory footprint with efficient pooling
- **Latency**: Sub-millisecond response times for simple endpoints
- **Concurrency**: Excellent scaling under high concurrent load

## Best Practices

### Endpoint Organization

Group related endpoints in static classes by feature:

```csharp
public static class UserEndpoints
{
    [Get("/api/users")]
    public static Result GetUsers() => Ok().Json(users);

    [Get("/api/users/{id}")]
    public static Result GetUser(int id) => Ok().Json(users[id]);

    [Post("/api/users")]
    public static Result CreateUser(CreateUserRequest req) => /* implementation */;
}

public static class PostEndpoints
{
    [Get("/api/posts")]
    public static Result GetPosts() => Ok().Json(posts);
    // ... more post endpoints
}
```

### Parameter Validation

Validate input parameters early:

```csharp
[Get("/api/users/{id}")]
public static Result GetUser(int id)
{
    if (id <= 0)
        return BadRequest().Text("ID must be positive");

    var user = FindUser(id);
    if (user == null)
        return NotFound().Text("User not found");

    return Ok().Json(user);
}
```

### Response Consistency

Establish consistent JSON response formats:

```csharp
private static ApiResponse<T> Success<T>(T data) =>
    new ApiResponse<T> { Success = true, Data = data };

private static ApiResponse Error(string message) =>
    new ApiResponse { Success = false, Message = message };

[Get("/api/data")]
public static Result GetData() => Ok().Json(Success(myData));
```

### Security Considerations

- Input validation is critical since binding uses minimal type conversion
- HTTPS should be handled by reverse proxy (nginx, IIS, etc.)
- Authentication/authorization must be implemented manually
- SQL injection protection when using raw database access

## Limitations

### Current Constraints

**Alpha Stage**: Celerio is currently in proof-of-concept stage and not recommended for production use.

**Limited Features**:
- No built-in middleware pipeline
- Manual content-type caring
- Basic parameter binding (no complex object deserialization)
- No dependency injection integration
- Minimal support for file uploads

**Performance Considerations**:
- Memory pooling may not scale as expected in all scenarios
- Buffer sizes may need fine-tuning for specific workloads
- Threading model optimized for high concurrency but simple endpoints

### Planned Enhancements

- [ ] Comprehensive parameter binding system
- [ ] Middleware pipeline support
- [ ] Built-in dependency injection
- [ ] File upload handling
- [ ] HTTPS support
- [ ] Authentication framework

## Contributing

Celerio is open-source and welcomes contributions. The project follows standard .NET development practices.

### Development Setup

```bash
git clone https://github.com/Oxule/Celerio.git
cd Celerio
dotnet build
```

### Contribution Guidelines

- Follow existing code style and naming conventions
- Add tests for new functionality
- Update documentation for API changes
- Ensure benchmark performance doesn't regress

### Contact

For technical discussions or questions, contact:
- **Author**: Oxule (Kirill Filonov)
- **Email**: ribb2017@mail.ru
- **Telegram**: [@Oxule](https://t.me/Oxule)

---

*Documentation last updated: v2.0.0*