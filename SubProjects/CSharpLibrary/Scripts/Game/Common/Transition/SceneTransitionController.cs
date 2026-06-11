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
        }
        if (transitionOut == null)
        {
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
            }
            else if (transitionState == TransitionState.TransitionIn)
            {
                transitionIn.enable = true;
                transitionOut.enable = false;
                transitionIn.Reset();
            }
            else
            {
                transitionIn.enable = false;
                transitionOut.enable = false;
            }
            prevState = transitionState;
        }
    }

}

