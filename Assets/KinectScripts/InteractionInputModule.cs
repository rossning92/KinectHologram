using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.EventSystems;


public class InteractionInputModule : PointerInputModule, InteractionListenerInterface
{
	private InteractionManager m_IntManager;
	private bool m_isLeftHandDrag = false;
	private Vector3 m_screenNormalPos = Vector3.zero;

	private PointerEventData.FramePressState m_framePressState = PointerEventData.FramePressState.NotChanged;
	private readonly MouseState m_MouseState = new MouseState();


	protected InteractionInputModule()
    {
	}


    [SerializeField]
    [FormerlySerializedAs("m_AllowActivationOnMobileDevice")]
    private bool m_ForceModuleActive;


    public bool forceModuleActive
    {
        get { return m_ForceModuleActive; }
        set { m_ForceModuleActive = value; }
    }

    public override bool IsModuleSupported()
    {
        return m_ForceModuleActive || InteractionManager.Instance != null;
    }

    public override bool ShouldActivateModule()
    {
        if (!base.ShouldActivateModule())
            return false;

        bool shouldActivate = m_ForceModuleActive;
	    shouldActivate |= (InteractionManager.Instance != null && InteractionManager.Instance.IsInteractionInited());

        return shouldActivate;
    }

    public override void ActivateModule()
    {
        base.ActivateModule();
	    
	    m_IntManager = InteractionManager.Instance;
		m_isLeftHandDrag = m_IntManager.IsLeftHandPrimary();

        var toSelect = eventSystem.currentSelectedGameObject;
        if (toSelect == null)
            toSelect = eventSystem.firstSelectedGameObject;

        eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
    }

    public override void DeactivateModule()
    {
        base.DeactivateModule();
        ClearSelection();
    }

    public override void Process()
    {
        ProcessInteractionEvent();
    }

	protected void ProcessInteractionEvent()
    {
		ProcessInteractionEvent(0);
    }

	protected void ProcessInteractionEvent(int id)
    {
		// Emulate mouse data
        var mouseData = GetMousePointerEventData(id);
        var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

        // Process the interaction data
		ProcessHandPressRelease(leftButtonData);
        ProcessMove(leftButtonData.buttonData);
        ProcessDrag(leftButtonData.buttonData);
    }

	protected override MouseState GetMousePointerEventData(int id)
	{
		// Populate the left button...
		PointerEventData leftData;
		var created = GetPointerData(kMouseLeftId, out leftData, true);

		leftData.Reset();

		m_screenNormalPos = m_isLeftHandDrag ? m_IntManager.GetLeftHandScreenPos() : m_IntManager.GetRightHandScreenPos();
		Vector2 handPos = new Vector2(m_screenNormalPos.x * Screen.width, m_screenNormalPos.y * Screen.height);

		if (created) 
		{
			leftData.position = handPos;
		}

		leftData.delta = handPos - leftData.position;
		leftData.position = handPos;
		//leftData.scrollDelta = 0f;
		leftData.button = PointerEventData.InputButton.Left;

		eventSystem.RaycastAll(leftData, m_RaycastResultCache);
		var raycast = FindFirstRaycast(m_RaycastResultCache);
		leftData.pointerCurrentRaycast = raycast;
		m_RaycastResultCache.Clear();

		m_MouseState.SetButtonState(PointerEventData.InputButton.Left, m_framePressState, leftData);
		m_framePressState = PointerEventData.FramePressState.NotChanged;

		return m_MouseState;
	}

    /// <summary>
    /// Process the current hand press or release event.
    /// </summary>
	protected void ProcessHandPressRelease(MouseButtonEventData data)
    {
        var pointerEvent = data.buttonData;
        var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (data.PressedThisFrame())
        {
            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            //Debug.Log("Pressed: " + newPressed);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                var diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
        }

        // PointerUp notification
        if (data.ReleasedThisFrame())
        {
            // Debug.Log("Executing pressup on: " + pointer.pointerPress);
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
			if (pointerEvent.pointerPress != null && pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
            }
            else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over somethign that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }
        }
    }


	public void HandGripDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
	{
		if (!isHandInteracting)
			return;

		m_framePressState = PointerEventData.FramePressState.Pressed;
		m_isLeftHandDrag = !isRightHand;
		m_screenNormalPos = handScreenPos;
	}

	public void HandReleaseDetected(long userId, int userIndex, bool isRightHand, bool isHandInteracting, Vector3 handScreenPos)
	{
		if (!isHandInteracting)
			return;

		m_framePressState = PointerEventData.FramePressState.Released;
		m_isLeftHandDrag = !isRightHand;
		m_screenNormalPos = handScreenPos;
	}

	public bool HandClickDetected(long userId, int userIndex, bool isRightHand, Vector3 handScreenPos)
	{
		return true;
	}


}

