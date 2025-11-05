# Elasticsearch Integration Guide

**Version:** 1.1.0  
**Last Updated:** October 30, 2025  
**Status:** MVP 1 Specification - Revised

---

> **Note on Implementation Details:** The design described in this document is a high-level approach. The specific fields to be indexed, the exact queries, and the worker implementation details are subject to change during the development of the Search Service.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Index Design](#index-design)
4. [Search Service Implementation](#search-service-implementation)
5. [Indexing Strategy](#indexing-strategy)
6. [Query Patterns](#query-patterns)
7. [Performance Optimization](#performance-optimization)
8. [Local Development Setup](#local-development-setup)

---

## Overview

ProperTea uses Elasticsearch for full-text search and autocomplete functionality across rental objects, contacts, and listings.

### Use Cases

- **Autocomplete** - Fast-as-you-type search for properties, rental objects, contacts
- **Full-Text Search** - Search by address, property name, tenant name
- **Filtered Search** - Combine text search with filters (bedrooms, price range, location)
- **Faceted Navigation** - Show available filters based on search results

### Design Decisions

**Why Elasticsearch?**
- ✅ Blazing fast full-text search (millisecond response times)
- ✅ Excellent autocomplete support (edge n-grams, fuzzy matching)
- ✅ Scalable (handles millions of documents)
- ✅ Rich query DSL
- ✅ Educational value (industry-standard search technology)

**Search Service Architecture:**
- Dedicated microservice owns Elasticsearch interaction
- Event-driven indexing (listens to PropertyCreated, ListingCreated, etc.)
- Separate API + Worker projects

---

## Architecture

```
┌─────────────────┐
│ Property Base   │ Publishes: RentalObjectCreated
│ Service         │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Market Service  │ Publishes: ListingCreated
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Contact Service │ Publishes: ContactCreated
└────────┬────────┘
         │
         ▼ (Events via Kafka)
┌─────────────────────────────┐
│ Search Service Worker        │
│ - Listens to events          │
│ - Indexes in Elasticsearch   │
└─────────────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│ Elasticsearch Cluster        │
│ - rental_objects index       │
│ - listings index             │
│ - contacts index             │
└─────────────────────────────┘
         ▲
         │
┌─────────────────────────────┐
│ Search Service API           │
│ - Autocomplete endpoint      │
│ - Search endpoint            │
└─────────────────────────────┘
```

---

## Index Design

### rental_objects Index

**Purpose:** Search for rental objects (apartments, parking spaces, etc.)

**Mapping:**

```json
{
  "mappings": {
    "properties": {
      "rentalObjectId": { "type": "keyword" },
      "propertyId": { "type": "keyword" },
      "companyId": { "type": "keyword" },
      "objectNumber": { "type": "keyword" },
      "type": { "type": "keyword" },
      "address": {
        "type": "text",
        "analyzer": "standard",
        "fields": {
          "autocomplete": {
            "type": "text",
            "analyzer": "autocomplete_analyzer",
            "search_analyzer": "standard"
          },
          "keyword": { "type": "keyword" }
        }
      },
      "propertyName": {
        "type": "text",
        "fields": {
          "autocomplete": {
            "type": "text",
            "analyzer": "autocomplete_analyzer"
          }
        }
      },
      "bedrooms": { "type": "integer" },
      "bathrooms": { "type": "integer" },
      "area": { "type": "float" },
      "floor": { "type": "integer" },
      "isVacant": { "type": "boolean" },
      "availableFrom": { "type": "date" },
      "availableTo": { "type": "date" },
      "createdAt": { "type": "date" },
      "updatedAt": { "type": "date" }
    }
  },
  "settings": {
    "analysis": {
      "analyzer": {
        "autocomplete_analyzer": {
          "type": "custom",
          "tokenizer": "standard",
          "filter": ["lowercase", "autocomplete_filter"]
        }
      },
      "filter": {
        "autocomplete_filter": {
          "type": "edge_ngram",
          "min_gram": 2,
          "max_gram": 20
        }
      }
    }
  }
}
```

### listings Index

**Purpose:** Search for market listings (available for rent)

**Mapping:**

```json
{
  "mappings": {
    "properties": {
      "listingId": { "type": "keyword" },
      "rentalObjectId": { "type": "keyword" },
      "address": {
        "type": "text",
        "fields": {
          "autocomplete": {
            "type": "text",
            "analyzer": "autocomplete_analyzer"
          },
          "keyword": { "type": "keyword" }
        }
      },
      "propertyName": { "type": "text" },
      "type": { "type": "keyword" },
      "bedrooms": { "type": "integer" },
      "bathrooms": { "type": "integer" },
      "monthlyRent": { "type": "float" },
      "area": { "type": "float" },
      "status": { "type": "keyword" },
      "isFeatured": { "type": "boolean" },
      "viewCount": { "type": "integer" },
      "publishedAt": { "type": "date" },
      "location": { "type": "geo_point" }
    }
  }
}
```

### contacts Index

**Purpose:** Search for contacts (tenants, property managers)

**Mapping:**

```json
{
  "mappings": {
    "properties": {
      "contactId": { "type": "keyword" },
      "userId": { "type": "keyword" },
      "fullName": {
        "type": "text",
        "fields": {
          "autocomplete": {
            "type": "text",
            "analyzer": "autocomplete_analyzer"
          },
          "keyword": { "type": "keyword" }
        }
      },
      "email": { "type": "keyword" },
      "phone": { "type": "keyword" },
      "organizationIds": { "type": "keyword" }
    }
  }
}
```

---

## Search Service Implementation

### Project Structure

```
services/Search/
├── ProperTea.Search.Service/          # API
│   ├── Endpoints/
│   │   └── SearchEndpoints.cs
│   ├── Services/
│   │   └── ElasticsearchService.cs
│   └── Program.cs
├── ProperTea.Search.Worker/           # Event consumer
│   ├── Handlers/
│   │   ├── RentalObjectIndexHandler.cs
│   │   ├── ListingIndexHandler.cs
│   │   └── ContactIndexHandler.cs
│   └── Program.cs
└── ProperTea.Search.Domain/           # Shared
    └── Models/
        ├── RentalObjectDocument.cs
        ├── ListingDocument.cs
        └── ContactDocument.cs
```

### API Service

**ElasticsearchService:**

```csharp
// ProperTea.Search.Service/Services/ElasticsearchService.cs
using Elastic.Clients.Elasticsearch;

public class ElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(ElasticsearchClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<AutocompleteResult>> AutocompleteAsync(string query, string indexName, int size = 10)
    {
        var response = await _client.SearchAsync<Dictionary<string, object>>(s => s
            .Index(indexName)
            .Size(size)
            .Query(q => q
                .MultiMatch(mm => mm
                    .Query(query)
                    .Fields(new[] { "address.autocomplete", "propertyName.autocomplete", "fullName.autocomplete" })
                    .Type(TextQueryType.BoolPrefix)
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            )
        );

        if (!response.IsValidResponse)
        {
            _logger.LogError("Elasticsearch query failed: {Error}", response.ElasticsearchServerError);
            return new List<AutocompleteResult>();
        }

        return response.Documents
            .Select(doc => new AutocompleteResult
            {
                Id = doc["rentalObjectId"]?.ToString() ?? doc["listingId"]?.ToString() ?? doc["contactId"]?.ToString(),
                Text = doc["address"]?.ToString() ?? doc["fullName"]?.ToString(),
                Type = indexName
            })
            .ToList();
    }

    public async Task<SearchResponse<T>> SearchAsync<T>(SearchRequest<T> request) where T : class
    {
        return await _client.SearchAsync(request);
    }

    public async Task IndexDocumentAsync<T>(string indexName, T document) where T : class
    {
        var response = await _client.IndexAsync(document, idx => idx.Index(indexName));
        
        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index document: {Error}", response.ElasticsearchServerError);
        }
    }

    public async Task BulkIndexAsync<T>(string indexName, List<T> documents) where T : class
    {
        var response = await _client.BulkAsync(b => b
            .Index(indexName)
            .IndexMany(documents)
        );

        if (!response.IsValidResponse)
        {
            _logger.LogError("Bulk indexing failed: {Error}", response.ElasticsearchServerError);
        }
    }
}
```

**Search Endpoints:**

```csharp
// ProperTea.Search.Service/Endpoints/SearchEndpoints.cs
public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/search");

        group.MapGet("/autocomplete", async (
            string query,
            string? type,
            ElasticsearchService esService) =>
        {
            var indexName = type switch
            {
                "rental_objects" => "rental_objects",
                "listings" => "listings",
                "contacts" => "contacts",
                _ => "rental_objects,listings,contacts" // Search all
            };

            var results = await esService.AutocompleteAsync(query, indexName);
            return Results.Ok(results);
        })
        .WithName("Autocomplete")
        .WithSummary("Autocomplete search across indexes");

        group.MapPost("/listings", async (
            ListingSearchRequest request,
            ElasticsearchService esService) =>
        {
            var searchRequest = new SearchRequest<ListingDocument>("listings")
            {
                Query = BuildListingQuery(request),
                Size = request.PageSize,
                From = (request.Page - 1) * request.PageSize,
                Sort = new[] { new SortOptions { Field = "publishedAt", Order = SortOrder.Desc } }
            };

            var response = await esService.SearchAsync(searchRequest);
            
            return Results.Ok(new
            {
                total = response.Total,
                page = request.Page,
                pageSize = request.PageSize,
                results = response.Documents
            });
        })
        .WithName("SearchListings")
        .WithSummary("Search rental listings with filters");

        return app;
    }

    private static QueryDescriptor<ListingDocument> BuildListingQuery(ListingSearchRequest request)
    {
        var queries = new List<Action<QueryDescriptor<ListingDocument>>>();

        // Text search
        if (!string.IsNullOrEmpty(request.Query))
        {
            queries.Add(q => q.MultiMatch(mm => mm
                .Query(request.Query)
                .Fields(new[] { "address^3", "propertyName^2" })
                .Fuzziness(new Fuzziness("AUTO"))
            ));
        }

        // Filters
        if (request.MinBedrooms.HasValue)
        {
            queries.Add(q => q.Range(r => r
                .Field(f => f.Bedrooms)
                .Gte(request.MinBedrooms.Value)
            ));
        }

        if (request.MaxRent.HasValue)
        {
            queries.Add(q => q.Range(r => r
                .Field(f => f.MonthlyRent)
                .Lte((double)request.MaxRent.Value)
            ));
        }

        if (request.Type != null)
        {
            queries.Add(q => q.Term(t => t.Field(f => f.Type).Value(request.Type)));
        }

        queries.Add(q => q.Term(t => t.Field(f => f.Status).Value("Published")));

        return new QueryDescriptor<ListingDocument>().Bool(b => b.Must(queries.ToArray()));
    }
}

public record ListingSearchRequest(
    string? Query,
    int? MinBedrooms,
    decimal? MaxRent,
    string? Type,
    int Page = 1,
    int PageSize = 20
);
```

### Worker Service

**Rental Object Index Handler:**

```csharp
// ProperTea.Search.Worker/Handlers/RentalObjectIndexHandler.cs
public class RentalObjectIndexHandler : IIntegrationEventHandler<RentalObjectCreatedEvent>
{
    private readonly ElasticsearchService _esService;
    private readonly ILogger<RentalObjectIndexHandler> _logger;

    public async Task HandleAsync(RentalObjectCreatedEvent @event, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("IndexRentalObject");
        activity?.SetTag("rentalObjectId", @event.RentalObjectId);

        var document = new RentalObjectDocument
        {
            RentalObjectId = @event.RentalObjectId.ToString(),
            PropertyId = @event.PropertyId.ToString(),
            CompanyId = @event.CompanyId.ToString(),
            ObjectNumber = @event.Data.ObjectNumber,
            Type = @event.Data.Type,
            Address = @event.Data.Address,
            PropertyName = @event.Data.PropertyName,
            Bedrooms = @event.Data.Bedrooms,
            Bathrooms = @event.Data.Bathrooms,
            Area = @event.Data.Area,
            Floor = @event.Data.Floor,
            IsVacant = @event.Data.IsVacant,
            AvailableFrom = @event.Data.AvailableFrom,
            AvailableTo = @event.Data.AvailableTo,
            CreatedAt = DateTime.UtcNow
        };

        await _esService.IndexDocumentAsync("rental_objects", document);
        
        _logger.LogInformation("Indexed rental object {RentalObjectId}", @event.RentalObjectId);
    }
}
```

---

## Indexing Strategy

### Event-Driven Indexing

**Flow:**
1. Property service creates rental object
2. Publishes `RentalObjectCreatedEvent` to Kafka
3. Search worker consumes event
4. Indexes document in Elasticsearch

**Advantages:**
- ✅ Decoupled (Property service doesn't know about search)
- ✅ Asynchronous (doesn't slow down API requests)
- ✅ Resilient (events stored in Kafka, retried on failure)

### Initial Indexing (Bulk)

**For existing data (migration):**

```csharp
// Search.Service/Jobs/InitialIndexJob.cs
public class InitialIndexJob
{
    private readonly HttpClient _propertyClient;
    private readonly ElasticsearchService _esService;

    public async Task IndexAllRentalObjectsAsync()
    {
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var response = await _propertyClient.GetAsync($"/api/rental-objects?page={page}&pageSize={pageSize}");
            var rentalObjects = await response.Content.ReadFromJsonAsync<List<RentalObjectDto>>();

            if (rentalObjects == null || rentalObjects.Count == 0)
                break;

            var documents = rentalObjects.Select(ro => new RentalObjectDocument
            {
                RentalObjectId = ro.Id.ToString(),
                // ... map fields
            }).ToList();

            await _esService.BulkIndexAsync("rental_objects", documents);
            
            page++;
        }
    }
}
```

### Reindexing Strategy

**When to reindex:**
- Index mapping changes (add new fields, change analyzers)
- Data inconsistencies detected
- Scheduled maintenance (e.g., monthly full reindex)

**Zero-Downtime Reindexing:**
1. Create new index: `rental_objects_v2`
2. Bulk index all data into `rental_objects_v2`
3. Switch alias: `rental_objects` → `rental_objects_v2`
4. Delete old index: `rental_objects_v1`

---

## Query Patterns

### Autocomplete

**Fast-as-you-type search:**

```http
GET /api/search/autocomplete?query=sunset&type=listings

Response:
{
  "results": [
    { "id": "listing-1", "text": "Sunset Apartments, 123 Main St", "type": "listings" },
    { "id": "listing-2", "text": "Sunset View Complex, 456 Oak Ave", "type": "listings" }
  ]
}
```

**Implementation uses edge n-grams:**
- Input: "sun" → Matches: "**Sun**set", "**Sun**ny"
- Input: "sunset ap" → Matches: "**Sunset Ap**artments"

### Filtered Search

**Combine text search + filters:**

```http
POST /api/search/listings
{
  "query": "downtown",
  "minBedrooms": 2,
  "maxRent": 2000,
  "type": "apartment"
}
```

### Geospatial Search

**Find listings near location:**

```csharp
var searchRequest = new SearchRequest<ListingDocument>("listings")
{
    Query = new GeoDistanceQuery
    {
        Field = "location",
        Distance = "5km",
        Location = new LatLonGeoLocation { Lat = 59.9139, Lon = 10.7522 } // Oslo coordinates
    }
};
```

---

## Performance Optimization

### Index Settings

**Number of shards:**
```json
{
  "settings": {
    "number_of_shards": 3,
    "number_of_replicas": 1
  }
}
```

**Refresh interval:**
```json
{
  "settings": {
    "refresh_interval": "30s"
  }
}
```

### Query Optimization

**1. Use filters instead of queries when possible:**
```csharp
// Good (cached)
.Filter(f => f.Term(t => t.Field(x => x.Status).Value("Published")))

// Less efficient (scored)
.Must(m => m.Match(mm => mm.Field(x => x.Status).Query("Published")))
```

**2. Limit field data:**
```csharp
.Source(s => s.Includes(i => i.Fields("rentalObjectId", "address", "monthlyRent")))
```

**3. Use pagination:**
```csharp
.From((page - 1) * pageSize)
.Size(pageSize)
```

---

## Local Development Setup

### docker-compose.infrastructure.yml

```yaml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
  environment:
    - discovery.type=single-node
    - xpack.security.enabled=false
    - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
  ports:
    - "9200:9200"
  volumes:
    - es-data:/usr/share/elasticsearch/data
  networks:
    - propertea-network

kibana:
  image: docker.elastic.co/kibana/kibana:8.11.0
  ports:
    - "5601:5601"
  depends_on:
    - elasticsearch
  environment:
    - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
  networks:
    - propertea-network
```

### Create Indexes

```bash
# Create rental_objects index
curl -X PUT "localhost:9200/rental_objects" -H 'Content-Type: application/json' -d @rental_objects_mapping.json

# Create listings index
curl -X PUT "localhost:9200/listings" -H 'Content-Type: application/json' -d @listings_mapping.json

# Verify
curl "localhost:9200/_cat/indices?v"
```

### Kibana Dev Tools

Access: http://localhost:5601/app/dev_tools#/console

**Test queries:**
```
GET rental_objects/_search
{
  "query": {
    "match": {
      "address.autocomplete": "sunset"
    }
  }
}
```

---

**Document Version:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-10-22 | Initial Elasticsearch integration guide |

