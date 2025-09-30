using System.Collections.Generic;

public interface ITrigger
{
    // Passes a HashSet of ITrigger to avoid recursive triggering
    public void Trigger(HashSet<ITrigger> triggered = null);
}
