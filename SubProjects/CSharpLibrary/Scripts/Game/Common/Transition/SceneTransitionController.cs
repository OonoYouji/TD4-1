using System.Collections.Generic;

class SceneTransitionController : MonoScript
{
    enum TransitionState
    {
        None = 0,
        TransitionOut = 1,
        TransitionIn = 2
    }

    [SerializeField]
    TransitionState transitionState = TransitionState.None;
    TransitionState prevState;

    TransitionIn transitionIn;
    TransitionOut transitionOut;
    public override void Initialize()
    {
        transitionIn = entity.GetScript<TransitionIn>();
        transitionOut = entity.GetScript<TransitionOut>();

        if (transitionIn == null)
        {
            Debug.LogError("TransitionIn script is missing.");
        }
        if (transitionOut == null)
        {
            Debug.LogError("TransitionOut script is missing.");
        }
    }

    public override void Update()
    {
        if (transitionIn == null || transitionOut == null)
        {
            return;
        }

        if (transitionState != prevState)
        {
            if (transitionState == TransitionState.TransitionOut)
            {
                transitionIn.enable = false;
                transitionOut.enable = true;
                transitionOut.Reset();
                Debug.LogInfo("Transition out");
            }
            else if (transitionState == TransitionState.TransitionIn)
            {
                transitionIn.enable = true;
                transitionOut.enable = false;
                transitionIn.Reset();
                Debug.LogInfo("Transition in");
            }
            else
            {
                transitionIn.enable = false;
                transitionOut.enable = false;
                Debug.LogInfo("Transition none");
            }
            prevState = transitionState;
        }
    }

}
