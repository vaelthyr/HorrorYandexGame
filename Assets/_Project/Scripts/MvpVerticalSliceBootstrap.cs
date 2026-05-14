using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AbsurdLiminalExpedition.Mvp
{
    /// <summary>
    /// Self-contained vertical slice for the absurd liminal expedition horror MVP.
    /// The scene intentionally contains no hand-wired objects: this bootstrap creates
    /// the hub, one Backrooms-like zone, first-person controls, objectives, audio tension,
    /// one zone rule, one simple threat, melee, and extraction at runtime.
    /// </summary>
    public sealed class MvpVerticalSliceBootstrap : MonoBehaviour
    {
        private const string SceneName = "MVPVerticalSlice";
        private const string CompletionSaveKey = "AbsurdLiminalExpedition.Mvp.CompletedBackroomsProcedure";

        private Transform _worldRoot;
        private Transform _playerRoot;
        private Camera _playerCamera;
        private MvpFirstPersonController _player;
        private MvpInteractionSystem _interaction;
        private MvpPlayerMelee _melee;
        private MvpHud _hud;
        private MvpObjectiveTracker _objectives;
        private MvpAudioTensionSystem _audio;
        private MvpZoneRuleController _zoneRule;
        private MvpThreat _threat;
        private MvpInteractable _hubPortalInteractable;

        private Material _floorMaterial;
        private Material _wallMaterial;
        private Material _hubMaterial;
        private Material _accentMaterial;
        private Material _dangerMaterial;
        private Material _safeMaterial;
        private Material _darkMaterial;
        private Material _portalMaterial;

        private bool _procedureAccepted;
        private bool _procedureCompleted;
        private bool _inZone;
        private bool _recording;
        private bool _failed;
        private bool _exitReady;
        private float _danger;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForMvpScene()
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (Object.FindFirstObjectByType<MvpVerticalSliceBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrap = new GameObject("MVP Vertical Slice Bootstrap");
            bootstrap.AddComponent<MvpVerticalSliceBootstrap>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Physics.gravity = new Vector3(0f, -22f, 0f);
        }

        private void Start()
        {
            CreateMaterials();
            CreatePlayerRig();
            CreateHud();
            CreateAudioSystem();
            BuildHub();
        }

        private void Update()
        {
            if (_failed)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    RestartProcedure();
                }

                return;
            }

            if (_inZone)
            {
                AddDanger(-Time.deltaTime * 0.018f);
            }

            if (Input.GetKeyDown(KeyCode.Escape) && _player != null)
            {
                _player.ToggleCursorLock();
            }
        }

        private void CreateMaterials()
        {
            _floorMaterial = CreateMaterial("MVP Damp Carpet", new Color(0.54f, 0.50f, 0.33f));
            _wallMaterial = CreateMaterial("MVP Yellowed Wall", new Color(0.83f, 0.77f, 0.48f));
            _hubMaterial = CreateMaterial("MVP Hub Concrete", new Color(0.22f, 0.25f, 0.28f));
            _accentMaterial = CreateMaterial("MVP Terminal Green", new Color(0.18f, 0.95f, 0.55f));
            _dangerMaterial = CreateMaterial("MVP Threat Red", new Color(0.95f, 0.08f, 0.06f));
            _safeMaterial = CreateMaterial("MVP Exit Cyan", new Color(0.16f, 0.85f, 1.00f));
            _darkMaterial = CreateMaterial("MVP Entity Dark", new Color(0.015f, 0.012f, 0.014f));
            _portalMaterial = CreateMaterial("MVP Portal Violet", new Color(0.45f, 0.24f, 0.95f));
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                shader = Shader.Find("Hidden/InternalErrorShader");
            }

            Material material = new Material(shader)
            {
                name = materialName
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.28f);
            }

            return material;
        }

        private void CreatePlayerRig()
        {
            GameObject player = new GameObject("MVP Player");
            player.transform.position = new Vector3(0f, 1.15f, -6.5f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.88f, 0f);
            controller.stepOffset = 0.38f;
            controller.slopeLimit = 48f;

            _player = player.AddComponent<MvpFirstPersonController>();

            GameObject cameraObject = new GameObject("MVP Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.56f, 0f);
            _playerCamera = cameraObject.AddComponent<Camera>();
            _playerCamera.fieldOfView = 74f;
            cameraObject.AddComponent<AudioListener>();

            Light flashlight = cameraObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 17f;
            flashlight.spotAngle = 58f;
            flashlight.intensity = 0.95f;
            flashlight.color = new Color(1f, 0.94f, 0.78f);
            flashlight.enabled = true;

            _player.Initialize(cameraObject.transform, flashlight);

            _interaction = cameraObject.AddComponent<MvpInteractionSystem>();
            _melee = cameraObject.AddComponent<MvpPlayerMelee>();

            _playerRoot = player.transform;
        }

        private void CreateHud()
        {
            GameObject hudObject = new GameObject("MVP HUD");
            _hud = hudObject.AddComponent<MvpHud>();
            _hud.Build();

            _objectives = new MvpObjectiveTracker();
            _objectives.Changed += () => _hud.SetObjectives(_objectives.ToDisplayText());

            _interaction.Initialize(_playerCamera.transform, _hud);
            _melee.Initialize(_playerCamera.transform, _hud);
        }

        private void CreateAudioSystem()
        {
            GameObject audioObject = new GameObject("MVP Procedural Audio Tension");
            _audio = audioObject.AddComponent<MvpAudioTensionSystem>();
            _audio.Initialize();
        }

        private void BuildHub()
        {
            _inZone = false;
            _failed = false;
            _recording = false;
            _exitReady = false;
            _danger = 0f;
            _threat = null;

            if (_zoneRule != null)
            {
                _zoneRule.StopRule();
            }

            ClearWorld("MVP Hub World");
            ConfigureRenderSettings(false);
            _audio.BeginHub();
            _player.SetMovementEnabled(true);
            _player.Teleport(new Vector3(0f, 1.15f, -7.5f), 0f);

            CreatePrimitive(PrimitiveType.Cube, "Hub Floor", new Vector3(0f, -0.08f, 0f), new Vector3(24f, 0.16f, 18f), _hubMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Cube, "Hub Back Wall", new Vector3(0f, 2.4f, 9f), new Vector3(24f, 4.8f, 0.35f), _hubMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Cube, "Hub Left Wall", new Vector3(-12f, 2.4f, 0f), new Vector3(0.35f, 4.8f, 18f), _hubMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Cube, "Hub Right Wall", new Vector3(12f, 2.4f, 0f), new Vector3(0.35f, 4.8f, 18f), _hubMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Cube, "Hub Ceiling", new Vector3(0f, 4.8f, 0f), new Vector3(24f, 0.22f, 18f), _hubMaterial, _worldRoot);

            CreateLight("Hub Cold Ceiling Light", new Vector3(0f, 4.25f, -2f), Color.white, 1.25f, 18f);
            CreateLight("Hub Portal Glow", new Vector3(6.2f, 2f, 5.8f), new Color(0.45f, 0.25f, 1f), 1.6f, 9f);

            GameObject terminal = CreatePrimitive(PrimitiveType.Cube, "Procedure Terminal", new Vector3(-4.7f, 0.9f, 4.9f), new Vector3(2.1f, 1.6f, 0.45f), _accentMaterial, _worldRoot);
            MvpInteractable terminalInteractable = terminal.AddComponent<MvpInteractable>();
            terminalInteractable.Prompt = "E - accept Procedure BR-01";
            terminalInteractable.Interacted += _ => AcceptProcedure();
            AddWorldText("TERMINAL\nBR-01: слушать, не бегать во время сирены", terminal.transform.position + new Vector3(0f, 1.3f, -0.28f), Quaternion.Euler(0f, 180f, 0f), 0.075f, Color.black);

            GameObject portal = CreatePrimitive(PrimitiveType.Cylinder, "Zone Entry Portal", new Vector3(6.2f, 1.45f, 5.8f), new Vector3(1.3f, 1.9f, 1.3f), _portalMaterial, _worldRoot);
            portal.AddComponent<MvpRotator>().DegreesPerSecond = new Vector3(0f, 38f, 0f);
            MvpInteractable portalInteractable = portal.AddComponent<MvpInteractable>();
            _hubPortalInteractable = portalInteractable;
            portalInteractable.Prompt = "E - enter Backrooms sector";
            portalInteractable.IsEnabled = _procedureAccepted;
            portalInteractable.Interacted += _ =>
            {
                if (!_procedureAccepted)
                {
                    _hud.SetStatus("Terminal procedure must be accepted first.", 2.4f);
                    return;
                }

                EnterBackroomsZone();
            };

            GameObject archive = CreatePrimitive(PrimitiveType.Cube, "Archive Pedestal", new Vector3(4.8f, 0.65f, -2.8f), new Vector3(1.6f, 1.3f, 1.6f), _hubMaterial, _worldRoot);
            MvpInteractable archiveInteractable = archive.AddComponent<MvpInteractable>();
            archiveInteractable.Prompt = "E - read archive note";
            archiveInteractable.Interacted += _ => _hud.SetStatus("Archive: BR-01 bends around sound. During siren, motion marks you.", 5f);

            CreatePrimitive(PrimitiveType.Cube, "Storage Crate A", new Vector3(-7.7f, 0.55f, -1.5f), new Vector3(1.5f, 1.1f, 1.5f), _hubMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Cube, "Storage Crate B", new Vector3(-8.2f, 0.35f, -3.2f), new Vector3(1.8f, 0.7f, 1.4f), _hubMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Capsule, "Emergency Pipe Visual", new Vector3(-2f, 0.75f, -4.6f), new Vector3(0.18f, 0.7f, 0.18f), _dangerMaterial, _worldRoot).transform.rotation = Quaternion.Euler(0f, 0f, 87f);

            _objectives.Reset(new MvpObjective("hub_accept", "Accept BR-01 procedure at the terminal"));
            if (_procedureAccepted)
            {
                _objectives.Complete("hub_accept");
                _objectives.Add(new MvpObjective("hub_enter", "Enter the Backrooms sector through the portal"));
            }

            string completion = PlayerPrefs.GetInt(CompletionSaveKey, 0) == 1 || _procedureCompleted
                ? " Previous extraction flag is saved. Terminal is ready for another run."
                : string.Empty;
            _hud.SetStatus("Hub online. Accept a procedure, then enter the portal." + completion, 5f);
            _hud.SetDanger(0f);
            _hud.SetRule("Rule: no active zone rule in hub.");
        }

        private void AcceptProcedure()
        {
            _procedureAccepted = true;
            if (_hubPortalInteractable != null)
            {
                _hubPortalInteractable.IsEnabled = true;
            }
            _objectives.Complete("hub_accept");
            _objectives.Add(new MvpObjective("hub_enter", "Enter the Backrooms sector through the portal"));
            _hud.SetStatus("Procedure BR-01 accepted. Objective: record the unstable room, activate beacon, extract.", 5f);
        }

        private void EnterBackroomsZone()
        {
            _objectives.Complete("hub_enter");
            BuildBackroomsZone();
        }

        private void BuildBackroomsZone()
        {
            _inZone = true;
            _failed = false;
            _recording = false;
            _exitReady = false;
            _danger = 0.12f;
            ClearWorld("MVP Backrooms Sector BR-01");
            ConfigureRenderSettings(true);
            _audio.BeginZone();
            _player.SetMovementEnabled(true);
            _player.Teleport(new Vector3(0f, 1.15f, -17.5f), 0f);

            BuildBackroomsGeometry();
            BuildBackroomsObjectives();
            BuildThreat();

            if (_zoneRule == null)
            {
                _zoneRule = gameObject.AddComponent<MvpZoneRuleController>();
            }
            _zoneRule.Initialize(_player, _hud, _audio, AddDanger);

            _objectives.Reset(
                new MvpObjective("zone_find_room", "Find the unstable room"),
                new MvpObjective("zone_record_audio", "Record the audio phenomenon"),
                new MvpObjective("zone_activate_beacon", "Activate the extraction beacon"),
                new MvpObjective("zone_extract", "Return through the extraction tear")
            );

            _hud.SetStatus("Entered BR-01. Listen first. The siren means stop moving.", 5f);
            _hud.SetDanger(_danger);
            _hud.SetRule("Rule BR-01: do not move during siren.");
        }

        private void BuildBackroomsGeometry()
        {
            CreatePrimitive(PrimitiveType.Cube, "Backrooms Carpet", new Vector3(0f, -0.08f, 0f), new Vector3(46f, 0.16f, 42f), _floorMaterial, _worldRoot);
            CreatePrimitive(PrimitiveType.Cube, "Backrooms Ceiling", new Vector3(0f, 3.08f, 0f), new Vector3(46f, 0.16f, 42f), _wallMaterial, _worldRoot);

            CreateWall("North Wall", new Vector3(0f, 1.55f, 21f), new Vector3(46f, 3.1f, 0.32f));
            CreateWall("South Wall", new Vector3(0f, 1.55f, -21f), new Vector3(46f, 3.1f, 0.32f));
            CreateWall("East Wall", new Vector3(23f, 1.55f, 0f), new Vector3(0.32f, 3.1f, 42f));
            CreateWall("West Wall", new Vector3(-23f, 1.55f, 0f), new Vector3(0.32f, 3.1f, 42f));

            CreateWall("Maze Wall 01", new Vector3(-14f, 1.55f, -10f), new Vector3(0.32f, 3.1f, 17f));
            CreateWall("Maze Wall 02", new Vector3(-7f, 1.55f, 1f), new Vector3(14f, 3.1f, 0.32f));
            CreateWall("Maze Wall 03", new Vector3(6f, 1.55f, -8f), new Vector3(0.32f, 3.1f, 19f));
            CreateWall("Maze Wall 04", new Vector3(13f, 1.55f, 6f), new Vector3(18f, 3.1f, 0.32f));
            CreateWall("Maze Wall 05", new Vector3(1f, 1.55f, 12f), new Vector3(0.32f, 3.1f, 12f));
            CreateWall("Maze Wall 06", new Vector3(-15f, 1.55f, 10f), new Vector3(10f, 3.1f, 0.32f));
            CreateWall("Maze Wall 07", new Vector3(16f, 1.55f, -11f), new Vector3(0.32f, 3.1f, 12f));
            CreateWall("Maze Wall 08", new Vector3(-3f, 1.55f, -15f), new Vector3(18f, 3.1f, 0.32f));

            for (int x = -18; x <= 18; x += 9)
            {
                for (int z = -15; z <= 15; z += 10)
                {
                    Light light = CreateLight("Buzzing Fluorescent " + x + ":" + z, new Vector3(x, 2.82f, z), new Color(1f, 0.91f, 0.58f), 0.75f, 7.5f);
                    light.gameObject.AddComponent<MvpFlickerLight>().Configure(0.55f, 1.05f, 0.9f + Mathf.Abs(x + z) * 0.02f);
                }
            }

            AddWorldText("BR-01\nTHE CARPET REMEMBERS STEPS", new Vector3(0f, 1.7f, -20.75f), Quaternion.identity, 0.08f, new Color(0.35f, 0.22f, 0.1f));
        }

        private void BuildBackroomsObjectives()
        {
            GameObject phenomenon = CreatePrimitive(PrimitiveType.Sphere, "Unstable Audio Phenomenon", new Vector3(18f, 1.4f, 14f), new Vector3(1.1f, 1.1f, 1.1f), _dangerMaterial, _worldRoot);
            phenomenon.AddComponent<MvpRotator>().DegreesPerSecond = new Vector3(31f, 52f, 17f);
            Light phenomenonLight = CreateLight("Unstable Room Pulse", new Vector3(18f, 2.2f, 14f), new Color(1f, 0.13f, 0.08f), 2f, 9f);
            phenomenonLight.gameObject.AddComponent<MvpFlickerLight>().Configure(0.7f, 2.4f, 4f);
            MvpInteractable phenomenonInteractable = phenomenon.AddComponent<MvpInteractable>();
            phenomenonInteractable.Prompt = "E - record audio phenomenon";
            phenomenonInteractable.Interacted += _ => StartCoroutine(RecordPhenomenonRoutine(phenomenonInteractable));
            AddWorldText("do not hum back", new Vector3(18f, 2.35f, 12.7f), Quaternion.Euler(0f, 180f, 0f), 0.055f, Color.black);

            GameObject beacon = CreatePrimitive(PrimitiveType.Cylinder, "Extraction Beacon", new Vector3(-18f, 0.9f, 15f), new Vector3(0.75f, 0.9f, 0.75f), _safeMaterial, _worldRoot);
            Light beaconLight = CreateLight("Beacon Dormant Light", new Vector3(-18f, 2.05f, 15f), new Color(0.15f, 0.85f, 1f), 0.15f, 4f);
            MvpInteractable beaconInteractable = beacon.AddComponent<MvpInteractable>();
            beaconInteractable.Prompt = "E - activate extraction beacon";
            beaconInteractable.Interacted += _ => ActivateBeacon(beaconLight);

            GameObject exit = CreatePrimitive(PrimitiveType.Cylinder, "Extraction Tear", new Vector3(0f, 1.45f, -18.8f), new Vector3(1.25f, 1.8f, 1.25f), _safeMaterial, _worldRoot);
            exit.SetActive(false);
            exit.AddComponent<MvpRotator>().DegreesPerSecond = new Vector3(0f, -70f, 0f);
            MvpInteractable exitInteractable = exit.AddComponent<MvpInteractable>();
            exitInteractable.Prompt = "E - extract to hub";
            exitInteractable.Interacted += _ => ExtractToHub();

            MvpExtractionGate gate = exit.AddComponent<MvpExtractionGate>();
            gate.Initialize(() => _exitReady);

            beaconInteractable.Interacted += _ => exit.SetActive(_exitReady);
        }

        private IEnumerator RecordPhenomenonRoutine(MvpInteractable interactable)
        {
            if (_recording)
            {
                yield break;
            }

            _recording = true;
            _player.SetMovementEnabled(false);
            _objectives.Complete("zone_find_room");
            _hud.SetStatus("Recording impossible audio... stay still.", 3f);
            _audio.StartRecordingTone();

            yield return new WaitForSeconds(3f);

            _audio.StopRecordingTone();
            _player.SetMovementEnabled(true);
            _objectives.Complete("zone_record_audio");
            interactable.IsEnabled = false;
            AddDanger(0.18f);
            _hud.SetStatus("Recording captured. Beacon coordinates decoded: northwest maintenance room.", 5f);
        }

        private void ActivateBeacon(Light beaconLight)
        {
            if (!_objectives.IsComplete("zone_record_audio"))
            {
                _hud.SetStatus("The beacon rejects silence. Record the unstable room first.", 3f);
                return;
            }

            if (_exitReady)
            {
                _hud.SetStatus("Beacon already active. Extraction tear is open near the entry corridor.", 3f);
                return;
            }

            _exitReady = true;
            beaconLight.intensity = 2.2f;
            beaconLight.range = 11f;
            _objectives.Complete("zone_activate_beacon");
            AddDanger(0.42f);
            ActivateThreat();
            _hud.SetStatus("Beacon active. Something heard it. Extract through the cyan tear near the entry corridor.", 5f);
        }

        private void ExtractToHub()
        {
            if (!_exitReady)
            {
                _hud.SetStatus("The tear is not stable yet. Activate the beacon.", 3f);
                return;
            }

            _objectives.Complete("zone_extract");
            _procedureCompleted = true;
            PlayerPrefs.SetInt(CompletionSaveKey, 1);
            PlayerPrefs.Save();
            BuildHub();
            _hud.SetStatus("Extraction complete. BR-01 progress saved. Reward: 1 unstable audio sample.", 6f);
        }

        private void BuildThreat()
        {
            GameObject threatObject = CreatePrimitive(PrimitiveType.Capsule, "MVP Entity - Corridor Listener", new Vector3(-20f, 1.1f, 18f), new Vector3(0.9f, 1.35f, 0.9f), _darkMaterial, _worldRoot);
            Light eye = CreateLight("Entity Eye", threatObject.transform.position + new Vector3(0f, 0.62f, 0.35f), new Color(1f, 0f, 0f), 0.8f, 4f);
            eye.transform.SetParent(threatObject.transform, true);
            _threat = threatObject.AddComponent<MvpThreat>();
            _threat.Initialize(_playerRoot, _hud, OnPlayerCaught);
            _threat.SetActiveThreat(false);
            _melee.SetThreatProvider(() => _threat);
        }

        private void ActivateThreat()
        {
            if (_threat == null)
            {
                return;
            }

            _threat.SetActiveThreat(true);
            _audio.SetDanger(Mathf.Max(_danger, 0.7f));
        }

        private void OnPlayerCaught()
        {
            if (_failed)
            {
                return;
            }

            _failed = true;
            _player.SetMovementEnabled(false);
            if (_zoneRule != null)
            {
                _zoneRule.StopRule();
            }

            _audio.SetDanger(1f);
            _hud.SetStatus("You were caught by the Corridor Listener. Press R to restart BR-01.", 999f);
        }

        private void RestartProcedure()
        {
            _hud.SetStatus("Restarting BR-01.", 2f);
            BuildBackroomsZone();
        }

        private void AddDanger(float amount)
        {
            float previous = _danger;
            _danger = Mathf.Clamp01(_danger + amount);
            _hud.SetDanger(_danger);
            _audio.SetDanger(_danger);

            if (_inZone && !_failed && _danger >= 0.72f && previous < 0.72f)
            {
                ActivateThreat();
                _hud.SetStatus("The rule noticed you. The Corridor Listener is active.", 4f);
            }
        }

        private void ClearWorld(string rootName)
        {
            if (_worldRoot != null)
            {
                Destroy(_worldRoot.gameObject);
            }

            _worldRoot = new GameObject(rootName).transform;
        }

        private void ConfigureRenderSettings(bool inBackrooms)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = inBackrooms ? 0.022f : 0.012f;
            RenderSettings.ambientLight = inBackrooms ? new Color(0.55f, 0.50f, 0.34f) : new Color(0.23f, 0.27f, 0.31f);
            RenderSettings.skybox = null;
        }

        private GameObject CreatePrimitive(PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = objectName;
            obj.transform.SetParent(parent, true);
            obj.transform.position = position;
            obj.transform.localScale = scale;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return obj;
        }

        private void CreateWall(string wallName, Vector3 position, Vector3 scale)
        {
            CreatePrimitive(PrimitiveType.Cube, wallName, position, scale, _wallMaterial, _worldRoot);
        }

        private Light CreateLight(string lightName, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(lightName);
            lightObject.transform.SetParent(_worldRoot, true);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }

        private void AddWorldText(string text, Vector3 position, Quaternion rotation, float characterSize, Color color)
        {
            GameObject textObject = new GameObject("World Text");
            textObject.transform.SetParent(_worldRoot, true);
            textObject.transform.position = position;
            textObject.transform.rotation = rotation;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 48;
            textMesh.color = color;
        }
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class MvpFirstPersonController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.1f;
        [SerializeField] private float sprintSpeed = 6.4f;
        [SerializeField] private float crouchSpeed = 2.15f;
        [SerializeField] private float jumpSpeed = 6.7f;
        [SerializeField] private float mouseSensitivity = 2.1f;
        [SerializeField] private float gravity = -22f;

        private CharacterController _controller;
        private Transform _cameraTransform;
        private Light _flashlight;
        private Vector3 _lastPlanarVelocity;
        private float _verticalVelocity;
        private float _pitch;
        private bool _movementEnabled = true;
        private bool _cursorLocked = true;

        public bool HasMoveInput { get; private set; }
        public float PlanarSpeed => new Vector2(_lastPlanarVelocity.x, _lastPlanarVelocity.z).magnitude;

        public void Initialize(Transform cameraTransform, Light flashlight)
        {
            _controller = GetComponent<CharacterController>();
            _cameraTransform = cameraTransform;
            _flashlight = flashlight;
            SetCursorLock(true);
        }

        private void Update()
        {
            HandleLook();
            HandleFlashlight();
            HandleMovement();
        }

        public void SetMovementEnabled(bool isEnabled)
        {
            _movementEnabled = isEnabled;
            if (!isEnabled)
            {
                HasMoveInput = false;
                _lastPlanarVelocity = Vector3.zero;
            }
        }

        public void Teleport(Vector3 position, float yaw)
        {
            if (_controller == null)
            {
                _controller = GetComponent<CharacterController>();
            }

            _controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _pitch = 0f;
            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.identity;
            }
            _verticalVelocity = 0f;
            _lastPlanarVelocity = Vector3.zero;
            _controller.enabled = true;
        }

        public void ToggleCursorLock()
        {
            SetCursorLock(!_cursorLocked);
        }

        private void SetCursorLock(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleLook()
        {
            if (!_cursorLocked || _cameraTransform == null)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX, Space.Self);
            _pitch = Mathf.Clamp(_pitch - mouseY, -84f, 84f);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleFlashlight()
        {
            if (_flashlight != null && Input.GetKeyDown(KeyCode.F))
            {
                _flashlight.enabled = !_flashlight.enabled;
            }
        }

        private void HandleMovement()
        {
            if (_controller == null)
            {
                _controller = GetComponent<CharacterController>();
            }

            Vector2 input = ReadMoveInput();
            HasMoveInput = input.sqrMagnitude > 0.01f;

            if (!_movementEnabled)
            {
                ApplyGravityOnly();
                return;
            }

            bool crouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
            bool sprinting = Input.GetKey(KeyCode.LeftShift) && !crouching && input.y > 0.1f;
            float targetHeight = crouching ? 1.15f : 1.75f;
            _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * 12f);
            _controller.center = new Vector3(0f, _controller.height * 0.5f, 0f);

            float speed = crouching ? crouchSpeed : sprinting ? sprintSpeed : walkSpeed;
            Vector3 move = (transform.right * input.x + transform.forward * input.y);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (_controller.isGrounded && Input.GetKeyDown(KeyCode.Space) && !crouching)
            {
                _verticalVelocity = jumpSpeed;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            _lastPlanarVelocity = move * speed;
            Vector3 velocity = _lastPlanarVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        private static Vector2 ReadMoveInput()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.W)) y += 1f;

            Vector2 input = new Vector2(x, y);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }
    }

    public sealed class MvpInteractionSystem : MonoBehaviour
    {
        private Transform _cameraTransform;
        private MvpHud _hud;
        private float _range = 3.4f;

        public void Initialize(Transform cameraTransform, MvpHud hud)
        {
            _cameraTransform = cameraTransform;
            _hud = hud;
        }

        private void Update()
        {
            if (_cameraTransform == null || _hud == null)
            {
                return;
            }

            if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _range, ~0, QueryTriggerInteraction.Collide))
            {
                MvpInteractable interactable = hit.collider.GetComponentInParent<MvpInteractable>();
                if (interactable != null && interactable.IsEnabled)
                {
                    _hud.SetInteraction(interactable.Prompt);
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        interactable.Interact(new MvpInteractionContext(gameObject, hit.point));
                    }
                    return;
                }
            }

            _hud.SetInteraction(string.Empty);
        }
    }

    public sealed class MvpInteractable : MonoBehaviour
    {
        public string Prompt = "E - interact";
        public bool IsEnabled = true;
        public event Action<MvpInteractionContext> Interacted;

        public void Interact(MvpInteractionContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            Interacted?.Invoke(context);
        }
    }

    public readonly struct MvpInteractionContext
    {
        public readonly GameObject Interactor;
        public readonly Vector3 HitPoint;

        public MvpInteractionContext(GameObject interactor, Vector3 hitPoint)
        {
            Interactor = interactor;
            HitPoint = hitPoint;
        }
    }

    public sealed class MvpPlayerMelee : MonoBehaviour
    {
        private Transform _cameraTransform;
        private MvpHud _hud;
        private Func<MvpThreat> _threatProvider;
        private float _cooldown;

        public void Initialize(Transform cameraTransform, MvpHud hud)
        {
            _cameraTransform = cameraTransform;
            _hud = hud;
        }

        public void SetThreatProvider(Func<MvpThreat> threatProvider)
        {
            _threatProvider = threatProvider;
        }

        private void Update()
        {
            if (_cooldown > 0f)
            {
                _cooldown -= Time.deltaTime;
                if (_cooldown <= 0f)
                {
                    _cooldown = 0f;
                }
                _hud?.SetMeleeCooldown(Mathf.Clamp01(_cooldown / 0.75f));
            }

            if (Input.GetMouseButtonDown(0) && _cooldown <= 0f)
            {
                Swing();
            }
        }

        private void Swing()
        {
            _cooldown = 0.75f;
            MvpThreat threat = _threatProvider != null ? _threatProvider() : null;
            if (threat == null || !threat.IsActiveThreat || _cameraTransform == null)
            {
                _hud?.SetStatus("Pipe swing cuts through stale air.", 1.2f);
                return;
            }

            Vector3 toThreat = threat.transform.position + Vector3.up * 0.7f - _cameraTransform.position;
            float distance = toThreat.magnitude;
            float facing = Vector3.Dot(_cameraTransform.forward, toThreat.normalized);
            if (distance <= 2.45f && facing > 0.62f)
            {
                threat.Stun(2.8f, _cameraTransform.forward * 4.2f);
                _hud?.SetStatus("Hit: Corridor Listener stunned. Move now.", 2f);
            }
            else
            {
                _hud?.SetStatus("Miss. It still hears you.", 1.4f);
            }
        }
    }

    public sealed class MvpThreat : MonoBehaviour
    {
        private Transform _target;
        private MvpHud _hud;
        private Action _caught;
        private float _stunTimer;
        private Vector3 _externalVelocity;
        private bool _activeThreat;

        public bool IsActiveThreat => _activeThreat;

        public void Initialize(Transform target, MvpHud hud, Action caught)
        {
            _target = target;
            _hud = hud;
            _caught = caught;
        }

        public void SetActiveThreat(bool active)
        {
            _activeThreat = active;
            gameObject.SetActive(active);
        }

        public void Stun(float seconds, Vector3 pushVelocity)
        {
            _stunTimer = Mathf.Max(_stunTimer, seconds);
            _externalVelocity = pushVelocity;
        }

        private void Update()
        {
            if (!_activeThreat || _target == null)
            {
                return;
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                transform.position += _externalVelocity * Time.deltaTime;
                _externalVelocity = Vector3.Lerp(_externalVelocity, Vector3.zero, Time.deltaTime * 3f);
                return;
            }

            Vector3 targetPosition = _target.position;
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > 0.01f)
            {
                Vector3 direction = toTarget / distance;
                float speed = distance > 8f ? 4.15f : 3.15f;
                transform.position += direction * speed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), Time.deltaTime * 9f);
            }

            if (distance <= 1.35f)
            {
                _hud?.SetStatus("The Corridor Listener is touching your shadow.", 1.5f);
                _caught?.Invoke();
            }
        }
    }

    public sealed class MvpZoneRuleController : MonoBehaviour
    {
        private MvpFirstPersonController _player;
        private MvpHud _hud;
        private MvpAudioTensionSystem _audio;
        private Action<float> _dangerDelta;
        private bool _running;
        private bool _sirenActive;
        private float _nextSirenTime;
        private float _sirenEndTime;

        public void Initialize(MvpFirstPersonController player, MvpHud hud, MvpAudioTensionSystem audio, Action<float> dangerDelta)
        {
            _player = player;
            _hud = hud;
            _audio = audio;
            _dangerDelta = dangerDelta;
            _running = true;
            _sirenActive = false;
            _nextSirenTime = Time.time + 9f;
            _hud?.SetRule("Rule BR-01: when siren starts, stop moving until it ends.");
        }

        public void StopRule()
        {
            _running = false;
            _sirenActive = false;
            _audio?.EndSiren();
        }

        private void Update()
        {
            if (!_running || _player == null)
            {
                return;
            }

            if (!_sirenActive && Time.time >= _nextSirenTime)
            {
                StartSiren();
            }

            if (_sirenActive)
            {
                bool moving = _player.HasMoveInput || _player.PlanarSpeed > 0.2f;
                if (moving)
                {
                    _dangerDelta?.Invoke(Time.deltaTime * 0.33f);
                    _hud?.SetRule("SIREN ACTIVE: stop moving. Danger is climbing.");
                }
                else
                {
                    _dangerDelta?.Invoke(-Time.deltaTime * 0.075f);
                    _hud?.SetRule("SIREN ACTIVE: good. Stay still.");
                }

                if (Time.time >= _sirenEndTime)
                {
                    EndSiren();
                }
            }
        }

        private void StartSiren()
        {
            _sirenActive = true;
            _sirenEndTime = Time.time + 6.5f;
            _audio?.StartSiren();
            _hud?.SetStatus("SIREN. Stop moving.", 2f);
        }

        private void EndSiren()
        {
            _sirenActive = false;
            _nextSirenTime = Time.time + UnityEngine.Random.Range(16f, 24f);
            _audio?.EndSiren();
            _hud?.SetRule("Rule BR-01: do not move during siren.");
            _hud?.SetStatus("Siren ended. Move.", 1.8f);
        }
    }

    public sealed class MvpAudioTensionSystem : MonoBehaviour
    {
        private AudioSource _ambient;
        private AudioSource _danger;
        private AudioSource _siren;
        private AudioSource _recording;
        private float _targetDanger;

        public void Initialize()
        {
            _ambient = gameObject.AddComponent<AudioSource>();
            _danger = gameObject.AddComponent<AudioSource>();
            _siren = gameObject.AddComponent<AudioSource>();
            _recording = gameObject.AddComponent<AudioSource>();

            _ambient.loop = true;
            _danger.loop = true;
            _siren.loop = true;
            _recording.loop = true;

            _ambient.clip = CreateClip("MVP Low Room Tone", 2f, t => 0.08f * Mathf.Sin(t * 2f * Mathf.PI * 58f) + 0.035f * Mathf.Sin(t * 2f * Mathf.PI * 91f));
            _danger.clip = CreateClip("MVP Pressure Drone", 2f, t => 0.12f * Mathf.Sin(t * 2f * Mathf.PI * 41f) + 0.08f * Mathf.Sin(t * 2f * Mathf.PI * 38.2f));
            _siren.clip = CreateClip("MVP Siren", 1.5f, t => 0.26f * Mathf.Sin(t * 2f * Mathf.PI * Mathf.Lerp(420f, 760f, Mathf.PingPong(t * 0.65f, 1f))));
            _recording.clip = CreateClip("MVP Recording Glitch", 1f, t => 0.18f * Mathf.Sin(t * 2f * Mathf.PI * 120f) * Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 9f)));

            _ambient.volume = 0.22f;
            _danger.volume = 0f;
            _siren.volume = 0f;
            _recording.volume = 0f;
            _ambient.Play();
            _danger.Play();
        }

        private void Update()
        {
            if (_danger != null)
            {
                _danger.volume = Mathf.Lerp(_danger.volume, Mathf.Lerp(0.02f, 0.48f, _targetDanger), Time.deltaTime * 2.5f);
                _danger.pitch = Mathf.Lerp(0.85f, 1.25f, _targetDanger);
            }

            if (_ambient != null)
            {
                _ambient.volume = Mathf.Lerp(_ambient.volume, Mathf.Lerp(0.18f, 0.32f, _targetDanger), Time.deltaTime * 1.5f);
            }
        }

        public void BeginHub()
        {
            SetDanger(0f);
            if (_ambient != null)
            {
                _ambient.pitch = 0.82f;
            }
            EndSiren();
            StopRecordingTone();
        }

        public void BeginZone()
        {
            SetDanger(0.15f);
            if (_ambient != null)
            {
                _ambient.pitch = 1f;
            }
            EndSiren();
            StopRecordingTone();
        }

        public void SetDanger(float normalizedDanger)
        {
            _targetDanger = Mathf.Clamp01(normalizedDanger);
        }

        public void StartSiren()
        {
            if (_siren == null)
            {
                return;
            }

            _siren.volume = 0.55f;
            if (!_siren.isPlaying)
            {
                _siren.Play();
            }
        }

        public void EndSiren()
        {
            if (_siren == null)
            {
                return;
            }

            _siren.Stop();
            _siren.volume = 0f;
        }

        public void StartRecordingTone()
        {
            if (_recording == null)
            {
                return;
            }

            _recording.volume = 0.32f;
            if (!_recording.isPlaying)
            {
                _recording.Play();
            }
        }

        public void StopRecordingTone()
        {
            if (_recording == null)
            {
                return;
            }

            _recording.Stop();
            _recording.volume = 0f;
        }

        private static AudioClip CreateClip(string clipName, float seconds, Func<float, float> sample)
        {
            const int sampleRate = 22050;
            int count = Mathf.Max(1, Mathf.CeilToInt(sampleRate * seconds));
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                data[i] = Mathf.Clamp(sample(t), -0.8f, 0.8f);
            }

            AudioClip clip = AudioClip.Create(clipName, count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    public sealed class MvpHud : MonoBehaviour
    {
        private Text _objectiveText;
        private Text _interactionText;
        private Text _statusText;
        private Text _dangerText;
        private Text _ruleText;
        private Text _meleeText;
        private float _statusUntil;

        public void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            }

            _objectiveText = CreateText("Objectives", font, new Vector2(24f, -24f), new Vector2(620f, 260f), TextAnchor.UpperLeft, 24, Color.white);
            _ruleText = CreateText("Rule", font, new Vector2(24f, -310f), new Vector2(720f, 84f), TextAnchor.UpperLeft, 24, new Color(1f, 0.88f, 0.55f));
            _dangerText = CreateText("Danger", font, new Vector2(-24f, -24f), new Vector2(460f, 82f), TextAnchor.UpperRight, 26, new Color(1f, 0.38f, 0.32f));
            _interactionText = CreateText("Interaction", font, new Vector2(0f, 88f), new Vector2(820f, 64f), TextAnchor.MiddleCenter, 28, Color.white);
            _statusText = CreateText("Status", font, new Vector2(0f, -88f), new Vector2(1120f, 92f), TextAnchor.MiddleCenter, 28, new Color(0.9f, 1f, 0.92f));
            _meleeText = CreateText("Melee", font, new Vector2(-24f, 72f), new Vector2(460f, 56f), TextAnchor.MiddleRight, 22, Color.white);

            Text crosshair = CreateText("Crosshair", font, Vector2.zero, new Vector2(60f, 60f), TextAnchor.MiddleCenter, 30, Color.white);
            crosshair.text = "+";
            SetInteraction(string.Empty);
            SetMeleeCooldown(0f);
        }

        private Text CreateText(string objectName, Font font, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor, int fontSize, Color color)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (anchor == TextAnchor.UpperRight || anchor == TextAnchor.MiddleRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void Update()
        {
            if (_statusText != null && _statusUntil > 0f && Time.time > _statusUntil)
            {
                _statusText.text = string.Empty;
                _statusUntil = 0f;
            }
        }

        public void SetObjectives(string text)
        {
            if (_objectiveText != null)
            {
                _objectiveText.text = text;
            }
        }

        public void SetInteraction(string text)
        {
            if (_interactionText != null)
            {
                _interactionText.text = text;
            }
        }

        public void SetStatus(string text, float seconds)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
                _statusUntil = seconds >= 900f ? float.PositiveInfinity : Time.time + Mathf.Max(0.1f, seconds);
            }
        }

        public void SetDanger(float normalizedDanger)
        {
            if (_dangerText != null)
            {
                _dangerText.text = "DANGER " + Mathf.RoundToInt(Mathf.Clamp01(normalizedDanger) * 100f) + "%";
            }
        }

        public void SetRule(string text)
        {
            if (_ruleText != null)
            {
                _ruleText.text = text;
            }
        }

        public void SetMeleeCooldown(float normalizedCooldown)
        {
            if (_meleeText != null)
            {
                if (normalizedCooldown <= 0.01f)
                {
                    _meleeText.text = "LMB: pipe ready";
                }
                else
                {
                    _meleeText.text = "LMB cooldown " + Mathf.CeilToInt(normalizedCooldown * 100f) + "%";
                }
            }
        }
    }

    public sealed class MvpObjectiveTracker
    {
        private readonly List<MvpObjective> _objectives = new List<MvpObjective>();
        public event Action Changed;

        public void Reset(params MvpObjective[] objectives)
        {
            _objectives.Clear();
            _objectives.AddRange(objectives);
            Changed?.Invoke();
        }

        public void Add(MvpObjective objective)
        {
            if (_objectives.Exists(item => item.Id == objective.Id))
            {
                return;
            }

            _objectives.Add(objective);
            Changed?.Invoke();
        }

        public bool Complete(string id)
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i].Id == id)
                {
                    if (_objectives[i].Completed)
                    {
                        return false;
                    }

                    _objectives[i] = _objectives[i].WithCompleted(true);
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool IsComplete(string id)
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i].Id == id)
                {
                    return _objectives[i].Completed;
                }
            }

            return false;
        }

        public string ToDisplayText()
        {
            if (_objectives.Count == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine("PROCEDURE OBJECTIVES");
            for (int i = 0; i < _objectives.Count; i++)
            {
                builder.Append(_objectives[i].Completed ? "[x] " : "[ ] ");
                builder.AppendLine(_objectives[i].Description);
            }

            return builder.ToString();
        }
    }

    public readonly struct MvpObjective
    {
        public readonly string Id;
        public readonly string Description;
        public readonly bool Completed;

        public MvpObjective(string id, string description, bool completed = false)
        {
            Id = id;
            Description = description;
            Completed = completed;
        }

        public MvpObjective WithCompleted(bool completed)
        {
            return new MvpObjective(Id, Description, completed);
        }
    }

    public sealed class MvpRotator : MonoBehaviour
    {
        public Vector3 DegreesPerSecond = new Vector3(0f, 45f, 0f);

        private void Update()
        {
            transform.Rotate(DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }

    public sealed class MvpFlickerLight : MonoBehaviour
    {
        private Light _light;
        private float _min;
        private float _max;
        private float _speed;
        private float _offset;

        public void Configure(float min, float max, float speed)
        {
            _light = GetComponent<Light>();
            _min = min;
            _max = max;
            _speed = speed;
            _offset = UnityEngine.Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (_light == null)
            {
                _light = GetComponent<Light>();
            }

            float pulse = Mathf.PerlinNoise(Time.time * _speed, _offset);
            _light.intensity = Mathf.Lerp(_min, _max, pulse);
        }
    }

    public sealed class MvpExtractionGate : MonoBehaviour
    {
        private Func<bool> _isReady;

        public void Initialize(Func<bool> isReady)
        {
            _isReady = isReady;
        }

        private void Update()
        {
            if (_isReady == null)
            {
                return;
            }

            bool ready = _isReady();
            if (gameObject.activeSelf != ready)
            {
                gameObject.SetActive(ready);
            }
        }
    }
}
