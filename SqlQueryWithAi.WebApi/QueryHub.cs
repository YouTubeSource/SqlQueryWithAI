using Azure.AI.OpenAI;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace SqlQueryWithAi.WebApi;

public class QueryHub : Hub
{
    private readonly OpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueryHub> _logger;
    private readonly string _connectionString;

    public QueryHub(
        OpenAIClient openAIClient, 
        IConfiguration configuration,
        ILogger<QueryHub> logger)
    {
        _openAIClient = openAIClient;
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    /// <summary>
    /// Main method to process natural language queries
    /// </summary>
    public async Task ProcessNaturalLanguageQuery(string naturalLanguageQuery)
    {
        try
        {
            _logger.LogInformation("Processing query: {Query}", naturalLanguageQuery);

            // Step 1: Send initial status
            await Clients.Caller.SendAsync("ReceiveStatus", "🤖 Analyzing your question...");

            // Step 2: Get database schema
            var schemaInfo = GetDatabaseSchema();

            // Step 3: Convert natural language to SQL using Azure OpenAI
            await Clients.Caller.SendAsync("ReceiveStatus", "🔄 Translating to SQL query...");
            var sqlQuery = await ConvertToSql(naturalLanguageQuery, schemaInfo);
            
            _logger.LogInformation("Generated SQL: {SQL}", sqlQuery);
            
            await Clients.Caller.SendAsync("ReceiveStatus", $"✅ SQL Generated");
            await Clients.Caller.SendAsync("ReceiveSqlQuery", sqlQuery);

            // Step 4: Execute the query
            await Clients.Caller.SendAsync("ReceiveStatus", "⚡ Executing query...");
            var results = await ExecuteQuery(sqlQuery);

            // Step 5: Send results back to client
            await Clients.Caller.SendAsync("ReceiveResults", results);
            await Clients.Caller.SendAsync("ReceiveStatus", 
                $"✅ Query completed! Found {results.Count} result(s)");
            
            _logger.LogInformation("Query completed successfully with {Count} results", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query");
            await Clients.Caller.SendAsync("ReceiveError", $"❌ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the database schema information for OpenAI context
    /// </summary>
    private string GetDatabaseSchema()
    {
        return @"
DATABASE SCHEMA:

Table: Orders
Columns:
- OrderID (int, Primary Key, Auto-increment)
- CustomerName (nvarchar(100), NOT NULL) - Name of the customer
- OrderDate (datetime, NOT NULL) - When the order was placed
- TotalAmount (decimal(10,2), NOT NULL) - Total order amount in dollars
- Status (nvarchar(50), NOT NULL) - Order status: 'Pending', 'Shipped', 'Completed'
- ShippingCity (nvarchar(100), NULL) - City where order ships to
- ProductCategory (nvarchar(50), NULL) - Category: 'Electronics', 'Clothing', 'Books', 'Sports', 'Home & Garden'

Sample data available:
- 25 orders total
- Order dates range from 75 days ago to yesterday
- Statuses: Completed, Pending, Shipped
- Various cities and product categories
";
    }

    /// <summary>
    /// Uses Azure OpenAI to convert natural language to SQL
    /// </summary>
    private async Task<string> ConvertToSql(string naturalLanguage, string schemaInfo)
    {
        var deploymentName = _configuration["AzureOpenAI:DeploymentName"];
        
        var systemPrompt = $@"You are an expert SQL query generator for SQL Server. Your job is to convert natural language questions into valid SQL queries.

{schemaInfo}

CRITICAL RULES:
1. Generate ONLY SELECT queries (no INSERT, UPDATE, DELETE, DROP, etc.)
2. Use proper SQL Server syntax and functions
3. For date calculations, use DATEADD and GETDATE()
4. Return ONLY the SQL query - no explanations, no markdown, no code blocks
5. Use appropriate aggregations (COUNT, SUM, AVG) when asked for totals or averages
6. Use WHERE clauses for filtering
7. Use GROUP BY for categorical analysis
8. Use ORDER BY for sorting results
9. Always use TOP when limiting results (e.g., TOP 5, TOP 10)

EXAMPLES:

Question: ""How many orders in the last 7 days?""
SQL: SELECT COUNT(*) AS OrderCount FROM Orders WHERE OrderDate >= DATEADD(day, -7, GETDATE())

Question: ""What is the total sales amount?""
SQL: SELECT SUM(TotalAmount) AS TotalSales FROM Orders

Question: ""Show me pending orders""
SQL: SELECT OrderID, CustomerName, OrderDate, TotalAmount FROM Orders WHERE Status = 'Pending' ORDER BY OrderDate DESC

Question: ""Top 5 customers by spending""
SQL: SELECT TOP 5 CustomerName, SUM(TotalAmount) AS TotalSpent, COUNT(*) AS OrderCount FROM Orders GROUP BY CustomerName ORDER BY TotalSpent DESC

Question: ""Average order value for completed orders""
SQL: SELECT AVG(TotalAmount) AS AverageOrderValue FROM Orders WHERE Status = 'Completed'

Question: ""Orders by category""
SQL: SELECT ProductCategory, COUNT(*) AS OrderCount, SUM(TotalAmount) AS TotalAmount FROM Orders GROUP BY ProductCategory ORDER BY TotalAmount DESC

Now convert this question to SQL:";

        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            DeploymentName = deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(naturalLanguage)
            },
            Temperature = 0.2f, // Low temperature for consistent SQL generation
            MaxTokens = 500,
            NucleusSamplingFactor = 0.95f
        };

        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        var sqlQuery = response.Value.Choices[0].Message.Content.Trim();
        
        // Clean up the response (remove markdown code blocks if present)
        sqlQuery = sqlQuery
            .Replace("```sql", "")
            .Replace("```", "")
            .Trim();
        
        // Additional cleanup
        if (sqlQuery.StartsWith("SQL:"))
        {
            sqlQuery = sqlQuery.Substring(4).Trim();
        }
        
        return sqlQuery;
    }

    /// <summary>
    /// Executes the SQL query and returns results
    /// </summary>
    private async Task<List<Dictionary<string, object>>> ExecuteQuery(string sqlQuery)
    {
        var results = new List<Dictionary<string, object>>();

        // SECURITY: Validate query is SELECT only
        var trimmedQuery = sqlQuery.Trim().ToUpper();
        if (!trimmedQuery.StartsWith("SELECT"))
        {
            throw new InvalidOperationException("Only SELECT queries are allowed for security reasons");
        }

        // Check for dangerous keywords
        var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "INSERT", "UPDATE", "ALTER", "CREATE", "EXEC", "EXECUTE" };
        foreach (var keyword in dangerousKeywords)
        {
            if (trimmedQuery.Contains(keyword))
            {
                throw new InvalidOperationException($"Query contains forbidden keyword: {keyword}");
            }
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = new SqlCommand(sqlQuery, connection)
        {
            CommandTimeout = 30 // 30 seconds timeout
        };
        
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.GetValue(i);
                
                // Convert DBNull to null for JSON serialization
                row[columnName] = value == DBNull.Value ? null! : value;
            }
            
            results.Add(row);
        }

        return results;
    }

    /// <summary>
    /// Called when client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("ReceiveStatus", "Connected to server ✅");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}