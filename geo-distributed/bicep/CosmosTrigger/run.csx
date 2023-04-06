using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

// Customize the model with your own desired properties
public class ToDoItem
{
    public string id { get; set; }
    public string Description { get; set; }
}

public static void Run(IReadOnlyList<ToDoItem> documents, ILogger log)
{
    log.LogInformation("Documents modified " + documents.Count);
    log.LogInformation("First document Id " + documents[0].id);
}