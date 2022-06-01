using System.Net;

namespace App.Public.DTO.v1;

public class RestApiErrorResponse
{
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public HttpStatusCode Status { get; set; }
    public string TraceId { get; set; } = default!;
    public Dictionary<string, List<string>> Errors { get; set; } = new();
    
}