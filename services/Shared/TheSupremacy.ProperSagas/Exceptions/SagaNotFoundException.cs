namespace TheSupremacy.ProperSagas.Exceptions;

public class SagaNotFoundException(Guid sagaId)
    : Exception($"Saga with ID {sagaId} was not found in the repository");