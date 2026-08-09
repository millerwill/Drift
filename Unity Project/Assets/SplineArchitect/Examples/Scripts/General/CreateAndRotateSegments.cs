using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SplineArchitect;
using SplineArchitect.Utility;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/General/CreateAndRotateSegments")]
    public class CreateAndRotateSegments : MonoBehaviour
    {
        public enum RotationState
        {
            IDLE,
            ROTATION_UPWARDS,
            ROTATION_DOWNWARDS,
        }

        [Header("General")]
        public Camera mainCamera;

        [Header("Spline")]
        public float segmentLength = 5;
        public GameObject pipePrefab;
        public GameObject pipeHolders;
        public GameObject pipeCover;

        [Header("Ui")]
        public Image addButton;
        public Sprite addButtonSprite;
        public Sprite addButtonHoverSprite;
        public Sprite addButtonPressSprite;
        public Image rotateRightButton;
        public Sprite rotateRightButtonSprite;
        public Sprite rotateRightButtonHoverSprite;
        public Sprite rotateRightButtonPressSprite;
        public Image rotateLeftButton;
        public Sprite rotateLeftButtonSprite;
        public Sprite rotateLeftButtonHoverSprite;
        public Sprite rotateLeftButtonPressSprite;
        public float rotationButtonsOffset;

        private RotationState rotationState = RotationState.IDLE;
        private Spline spline;
        private bool mouseDown;

        void Start()
        {
            UpdateButtonPositions(transform.position, transform.rotation);
        }

        void Update()
        {
            if(!mouseDown) 
                addButton.sprite = addButtonSprite;

            if (HandleInput.IsMouseOverRect((RectTransform)addButton.transform))
            {
                if (!mouseDown)
                    addButton.sprite = addButtonHoverSprite;
                if(HandleInput.IsMouseLeftDown())
                {
                    addButton.sprite = addButtonPressSprite;
                    AddSegment();
                    mouseDown = true;
                }
            }

            if (spline != null && spline.SegmentCount > 0)
            {
                if (!mouseDown)
                    rotateRightButton.sprite = rotateRightButtonSprite;
                if (HandleInput.IsMouseOverRect((RectTransform)rotateRightButton.transform))
                {
                    if (!mouseDown)
                        rotateRightButton.sprite = rotateRightButtonHoverSprite;
                    if (HandleInput.IsMouseLeftDown())
                    {
                        rotateRightButton.sprite = rotateRightButtonPressSprite;
                        rotationState = RotationState.ROTATION_UPWARDS;
                        mouseDown = true;
                    }
                }

                if (!mouseDown)
                    rotateLeftButton.sprite = rotateLeftButtonSprite;
                if (HandleInput.IsMouseOverRect((RectTransform)rotateLeftButton.transform))
                {
                    if (!mouseDown)
                        rotateLeftButton.sprite = rotateLeftButtonHoverSprite;
                    if (HandleInput.IsMouseLeftDown())
                    {
                        rotateLeftButton.sprite = rotateLeftButtonPressSprite;
                        rotationState = RotationState.ROTATION_DOWNWARDS;
                        mouseDown = true;
                    }
                }
            }

            if (HandleInput.IsMouseLeftUp())
            {
                rotationState = RotationState.IDLE;
                mouseDown = false;
            }

            if (rotationState == RotationState.IDLE)
                return;

            Segment last = spline.GetSegmentAtIndex(spline.SegmentCount - 1);

            // Rotate
            if (rotationState == RotationState.ROTATION_UPWARDS)
            {
                last.Rotate(new Vector3(1, 0, 0));
                UpdateButtonPositions(last.GetPosition(ControlHandle.ANCHOR), last.Rotation);
            }
            else if(rotationState == RotationState.ROTATION_DOWNWARDS)
            {
                last.Rotate(new Vector3(-1, 0, 0));
                UpdateButtonPositions(last.GetPosition(ControlHandle.ANCHOR), last.Rotation);
            }
        }

        private void UpdateButtonPositions(Vector3 point, Quaternion rotation)
        {
            //Add button
            Vector3 addButtonPos = mainCamera.WorldToScreenPoint(point - rotation * Vector3.forward * 2);
            addButton.transform.position = addButtonPos;

            if(spline != null && spline.SegmentCount > 0)
            {
                // Rotate right button
                rotateRightButton.gameObject.SetActive(true);
                Vector3 rotateUpwardsButtonPos = mainCamera.WorldToScreenPoint(point + rotation * Vector3.down * rotationButtonsOffset);
                rotateRightButton.transform.position = rotateUpwardsButtonPos;

                // Rotate left button
                rotateLeftButton.gameObject.SetActive(true);
                Vector3 rotateDownwardsButtonPos = mainCamera.WorldToScreenPoint(point + rotation * Vector3.up * rotationButtonsOffset);
                rotateLeftButton.transform.position = rotateDownwardsButtonPos;
            }
        }

        public void AddSegment()
        {
            if (spline == null)
            {
                // Create spline
                Vector3 anchor = transform.position;
                Vector3 anchor2 = transform.position + (transform.rotation * Vector3.forward) * segmentLength;
                float tangentLength = segmentLength * 0.25f;
                spline = SplineUtility.Create(new GameObject("Platform Spline"), anchor, tangentLength, tangentLength, 
                    transform.rotation, anchor2, tangentLength, tangentLength, transform.rotation);
                spline.RenderInGame = true;
                spline.SetSplineResolution(1000);

                // Pipe prefab
                Population population = new Population(pipePrefab, true);
                population.FillMode = PopulationFillMode.SNAP_LAST;
                population.Resolution = 6;
                spline.AddPopulation(population);

                // Pipe holders
                Population population2 = new Population(pipeHolders, false);
                population2.Spacing = 10;
                population2.PrefabRotationOffset = Quaternion.Euler(0, 0, 90);
                population2.XOffset = 0.78f;
                spline.AddPopulation(population2);

                // Pipe covers
                GameObject cover = Object.Instantiate(pipeCover);
                SplineObject follower = spline.CreateFollower(cover, Vector3.zero, Quaternion.Euler(0,180,0));
                cover = Object.Instantiate(pipeCover);
                follower = spline.CreateFollower(cover, Vector3.zero, Quaternion.Euler(0, 180, 0));
                follower.AlignToEnd = true;

                UpdateButtonPositions(anchor2, transform.rotation);
            }
            else
            {
                // Create segment
                Segment prevSegment = spline.GetSegmentAtIndex(spline.SegmentCount - 1);
                Vector3 prevAnchor = prevSegment.GetPosition(ControlHandle.ANCHOR);
                Quaternion prevRotation = prevSegment.Rotation;
                Vector3 newAnchor = prevAnchor + (prevRotation * Vector3.forward * segmentLength);
                float tangentLength = segmentLength * 0.25f;
                spline.CreateSegment(newAnchor, tangentLength, tangentLength, prevRotation);

                UpdateButtonPositions(newAnchor, prevRotation);
            }
        }
    }
}
