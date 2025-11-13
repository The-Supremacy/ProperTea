using TheSupremacy.ProperSagas.Orchestration;

namespace TheSupremacy.ProperSagas.Services;

public class SagaRegistry
{
    private readonly Dictionary<string, Type> _orchestratorTypes = new();

    public void Register<TOrchestrator>(string sagaType)
        where TOrchestrator : SagaOrchestratorBase
    {
        _orchestratorTypes[sagaType] = typeof(TOrchestrator);
    }

    public Type? GetOrchestratorType(string sagaType)
    {
        return _orchestratorTypes.GetValueOrDefault(sagaType);
    }

    public bool IsRegistered(string sagaType)
    {
        return _orchestratorTypes.ContainsKey(sagaType);
    }
}