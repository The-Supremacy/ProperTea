using TheSupremacy.ProperDomain.Events;

namespace TheSupremacy.ProperDomain.UnitTests.TestHelpers;

public record TestDomainEvent(Guid EventId, DateTime OccurredAt) : IDomainEvent;

public record TestAggregateDomainEvent(Guid EventId, DateTime OccurredAt, Guid AggregateId) : IDomainEvent;