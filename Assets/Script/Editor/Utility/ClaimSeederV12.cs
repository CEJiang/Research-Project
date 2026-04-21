#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ClaimSeederV12
{
    private const string OutputFolder = "Assets/Resources/Claims";

    // ✅ ClaimId -> requiredFeatures (atomic default)
    public static readonly List<string> features = new()
    {
        // =========================================================
        // Outdoor / boundary
        // =========================================================
        "structural_deformation",
        "localized_damage",
        "non_uniform_force",
        "directed_movement",
        "continuous_path",
        "movement_variation",
        "structural_integrity",
        "consistent_alignment",
        "environmental_normalcy",

        // =========================================================
        // Occupancy / storage / access
        // =========================================================
        "recent_use",
        "post_use_state",
        "localized_irregularity",
        "storage_absence",
        "selective_removal",
        "missing_personal_trace",
        "limited_personal_presence",
        "partial_open_state",
        "undisturbed_container",
        "missing_expected_content",

        // =========================================================
        // Interior disturbance / aftermath
        // =========================================================
        "object_displacement",
        "environmental_disturbance",
        "uncleared_activity",
        "absence_of_recent_use",
        "scattered_objects",
        "intact_after_disturbance",

        // =========================================================
        // Departure / lobby / monitoring
        // =========================================================
        "unused_departure_items",
        "grouped_personal_items",
        "unexecuted_preparation",
        "system_failure",
        "surveillance_unavailable",
        "visual_obstruction",
        "routine_entry_order",
        "entry_zone_disturbance",
        "foot_traffic_trace",

        // =========================================================
        // Writing / planning / time
        // =========================================================
        "written_reminder",
        "checklist_structure",
        "recent_writing_activity",
        "planned_behavior",
        "fixed_time_state",
        "stopped_mechanism",
        "time_anchor",
        "documented_schedule",
        "routine_structure",
        "time_specific_plan",

        // =========================================================
        // Formal documents / interruption / workstation
        // =========================================================
        "physical_condition_record",
        "physical_normal_state",
        "formal_record_structure",
        "timestamped_record",
        "unfinished_communication",
        "incomplete_task",
        "interrupted_action",
        "hesitation_in_writing",
        "inactive_device_state",
        "disconnected_power",
        "undisturbed_workstation",

        // =========================================================
        // Travel / secondary report
        // =========================================================
        "confirmed_departure_plan",
        "travel_preparation",
        "one_way_travel_indicator",
        "secondary_report_source",
        "incident_reference",
        "house_related_report",
    };

    public static readonly Dictionary<string, List<string>> featureClaimToRequiredFeatures = new()
    {
        // =========================================================
        // C1a: Boundary deformation
        // =========================================================
        { "C1a1", new() { "structural_deformation" } },
        { "C1a2", new() { "localized_damage" } },
        { "C1a3", new() { "non_uniform_force" } },

        // =========================================================
        // C1b: Outdoor movement traces
        // =========================================================
        { "C1b1", new() { "directed_movement" } },
        { "C1b2", new() { "continuous_path" } },
        { "C1b3", new() { "movement_variation" } },

        // =========================================================
        // C1c: Outdoor normal baseline
        // =========================================================
        { "C1c1", new() { "structural_integrity" } },
        { "C1c2", new() { "consistent_alignment" } },
        { "C1c3", new() { "environmental_normalcy" } },

        // =========================================================
        // C2a: Recent use / post-use
        // =========================================================
        { "C2a1", new() { "recent_use" } },
        { "C2a2", new() { "post_use_state" } },
        { "C2a3", new() { "localized_irregularity" } },

        // =========================================================
        // C2b: Removal / absence
        // =========================================================
        { "C2b1", new() { "storage_absence" } },
        { "C2b2", new() { "selective_removal" } },
        { "C2b3", new() { "missing_personal_trace" } },
        { "C2b4", new() { "missing_expected_content" } },

        // =========================================================
        // C2c: Partial access / incomplete presence
        // =========================================================
        { "C2c1", new() { "limited_personal_presence" } },
        { "C2c2", new() { "partial_open_state" } },
        { "C2c3", new() { "undisturbed_container" } },

        // =========================================================
        // C3a: Disturbance / displacement
        // =========================================================
        { "C3a1", new() { "object_displacement" } },
        { "C3a2", new() { "environmental_disturbance" } },
        { "C3a3", new() { "scattered_objects" } },
        { "C3a4", new() { "intact_after_disturbance" } },

        // =========================================================
        // C3b: Activity remains / baseline absence
        // =========================================================
        { "C3b1", new() { "uncleared_activity" } },
        { "C3b2", new() { "absence_of_recent_use" } },

        // =========================================================
        // C4a: Departure execution
        // =========================================================
        { "C4a1", new() { "unused_departure_items" } },
        { "C4a2", new() { "grouped_personal_items" } },
        { "C4a3", new() { "unexecuted_preparation" } },

        // =========================================================
        // C4b: Monitoring / system state
        // =========================================================
        { "C4b1", new() { "system_failure" } },
        { "C4b2", new() { "surveillance_unavailable" } },
        { "C4b3", new() { "visual_obstruction" } },

        // =========================================================
        // C4c: Entry zone
        // =========================================================
        { "C4c1", new() { "routine_entry_order" } },
        { "C4c2", new() { "entry_zone_disturbance" } },
        { "C4c3", new() { "foot_traffic_trace" } },

        // =========================================================
        // C5a: Written reminders / planning
        // =========================================================
        { "C5a1", new() { "written_reminder" } },
        { "C5a2", new() { "checklist_structure" } },
        { "C5a3", new() { "recent_writing_activity" } },
        { "C5a4", new() { "planned_behavior" } },

        // =========================================================
        // C5b: Fixed time / stopped mechanism
        // =========================================================
        { "C5b1", new() { "fixed_time_state" } },
        { "C5b2", new() { "stopped_mechanism" } },
        { "C5b3", new() { "time_anchor" } },

        // =========================================================
        // C5c: Schedule / time plan
        // =========================================================
        { "C5c1", new() { "documented_schedule" } },
        { "C5c2", new() { "routine_structure" } },
        { "C5c3", new() { "time_specific_plan" } },

        // =========================================================
        // C6a: Medical / formal record
        // =========================================================
        { "C6a1", new() { "physical_condition_record" } },
        { "C6a2", new() { "physical_normal_state" } },
        { "C6a3", new() { "formal_record_structure" } },
        { "C6a4", new() { "timestamped_record" } },

        // =========================================================
        // C6b: Interrupted writing
        // =========================================================
        { "C6b1", new() { "unfinished_communication" } },
        { "C6b2", new() { "incomplete_task" } },
        { "C6b3", new() { "interrupted_action" } },
        { "C6b4", new() { "hesitation_in_writing" } },

        // =========================================================
        // C6c: Workstation state
        // =========================================================
        { "C6c1", new() { "inactive_device_state" } },
        { "C6c2", new() { "disconnected_power" } },
        { "C6c3", new() { "undisturbed_workstation" } },

        // =========================================================
        // C7a: Travel confirmation
        // =========================================================
        { "C7a1", new() { "confirmed_departure_plan" } },
        { "C7a2", new() { "travel_preparation" } },
        { "C7a3", new() { "one_way_travel_indicator" } },

        // =========================================================
        // C7b: Secondary report
        // =========================================================
        { "C7b1", new() { "secondary_report_source" } },
        { "C7b2", new() { "incident_reference" } },
        { "C7b3", new() { "house_related_report" } },
    };

    private class ClaimSeed
    {
        public string id;
        public string domainId;
        public string description;

        // ✅ NEW: this drives matching (AND gate)
        public List<string> requiredFeatures = new();

        public List<ClaimEffect> effects = new();
    }

    [MenuItem("Narrative/Claims/Generate v1.2 Atomic Claims")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Claims");

        var seeds = BuildSeeds();

        int created = 0, updated = 0;

        foreach (var s in seeds)
        {
            string path = $"{OutputFolder}/{s.id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Claim>(path);

            if (existing == null)
            {
                var claim = ScriptableObject.CreateInstance<Claim>();
                claim.id = s.id;
                claim.domainId = s.domainId;
                claim.description = s.description;

                // ✅ IMPORTANT: Claim script must have this field:
                // public List<string> requiredFeatures;
                claim.requiredFeatures = new List<string>(s.requiredFeatures);

                claim.effects = new List<ClaimEffect>(s.effects);

                AssetDatabase.CreateAsset(claim, path);
                EditorUtility.SetDirty(claim);
                created++;
            }
            else
            {
                existing.id = s.id;
                existing.domainId = s.domainId;
                existing.description = s.description;

                existing.requiredFeatures = new List<string>(s.requiredFeatures);
                existing.effects = new List<ClaimEffect>(s.effects);

                EditorUtility.SetDirty(existing);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ClaimSeederV12] Done. Created={created}, Updated={updated}, Total={seeds.Count}");
    }

    private static List<ClaimSeed> BuildSeeds()
    {
        var L = new List<ClaimSeed>();

        ClaimEffect E(string hid, Polarity p, float w) => new ClaimEffect
        {
            hypothesisId = hid,
            polarity = p,
            weight = w
        };

        ClaimSeed S(string id, string domainId, string description, params ClaimEffect[] effects)
        {
            var seed = new ClaimSeed
            {
                id = id,
                domainId = domainId,
                description = description,
                effects = effects.ToList(),
                requiredFeatures = new List<string>()
            };

            if (featureClaimToRequiredFeatures.TryGetValue(id, out var sig) && sig != null)
                seed.requiredFeatures.AddRange(sig.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

            return seed;
        }

        // =========================================================
        // D1: Outdoor boundary and movement
        // =========================================================
        L.Add(S("C1a1", "D1",
            "A boundary structure shows visible deformation.",
            E("H2", Polarity.Support, 0.6f)));

        L.Add(S("C1a2", "D1",
            "Damage is localized to a specific part of the structure.",
            E("H2", Polarity.Support, 0.5f),
            E("H3", Polarity.Support, 0.2f)));

        L.Add(S("C1a3", "D1",
            "The deformation pattern is uneven rather than uniformly distributed.",
            E("H2", Polarity.Support, 0.4f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C1b1", "D1",
            "Movement traces indicate a consistent direction of travel.",
            E("H2", Polarity.Support, 0.5f),
            E("H4", Polarity.Counter, 0.3f)));

        L.Add(S("C1b2", "D1",
            "A continuous path of movement is visible on the ground.",
            E("H2", Polarity.Support, 0.6f),
            E("H4", Polarity.Counter, 0.4f)));

        L.Add(S("C1b3", "D1",
            "Variations in trace depth suggest changes in movement state.",
            E("H2", Polarity.Support, 0.3f)));

        L.Add(S("C1c1", "D1",
            "A nearby boundary structure remains intact.",
            E("H2", Polarity.Counter, 0.4f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C1c2", "D1",
            "The surrounding structure maintains a consistent alignment.",
            E("H2", Polarity.Counter, 0.3f)));

        L.Add(S("C1c3", "D1",
            "Part of the outdoor environment appears normal and undisturbed.",
            E("H2", Polarity.Counter, 0.3f),
            E("H0", Polarity.Support, 0.2f)));

        // =========================================================
        // D2: Occupancy / storage / personal traces
        // =========================================================
        L.Add(S("C2a1", "D2",
            "The area shows signs of recent use.",
            E("H4", Polarity.Support, 0.4f),
            E("H1", Polarity.Counter, 0.2f)));

        L.Add(S("C2a2", "D2",
            "The area remains in a post-use state without full reset.",
            E("H4", Polarity.Support, 0.5f),
            E("H1", Polarity.Counter, 0.3f)));

        L.Add(S("C2a3", "D2",
            "A localized irregularity is present within an otherwise usable space.",
            E("H2", Polarity.Support, 0.3f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C2b1", "D2",
            "A storage space is currently empty despite evidence of prior use.",
            E("H1", Polarity.Support, 0.4f),
            E("H3", Polarity.Support, 0.4f)));

        L.Add(S("C2b2", "D2",
            "The pattern suggests selective removal rather than complete clearance.",
            E("H3", Polarity.Support, 0.7f),
            E("H1", Polarity.Support, 0.3f)));

        L.Add(S("C2b3", "D2",
            "Expected traces of ordinary personal occupancy are missing.",
            E("H1", Polarity.Support, 0.5f),
            E("H3", Polarity.Support, 0.4f)));

        L.Add(S("C2b4", "D2",
            "Expected contents are absent while the container remains intact.",
            E("H3", Polarity.Support, 0.8f)));

        L.Add(S("C2c1", "D2",
            "Only a limited subset of personal-use items is present.",
            E("H3", Polarity.Support, 0.5f),
            E("H2", Polarity.Support, 0.2f)));

        L.Add(S("C2c2", "D2",
            "A container is left partially open.",
            E("H2", Polarity.Support, 0.2f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C2c3", "D2",
            "An accessed storage area shows no strong signs of full rummaging.",
            E("H3", Polarity.Support, 0.6f)));

        // =========================================================
        // D3: Interior disturbance / aftermath
        // =========================================================
        L.Add(S("C3a1", "D3",
            "Objects are displaced from their normal positions.",
            E("H2", Polarity.Support, 0.6f),
            E("H3", Polarity.Support, 0.2f)));

        L.Add(S("C3a2", "D3",
            "The local environment shows signs of disturbance.",
            E("H2", Polarity.Support, 0.6f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C3a3", "D3",
            "Loose objects are scattered across the area.",
            E("H2", Polarity.Support, 0.5f)));

        L.Add(S("C3a4", "D3",
            "Objects remain intact despite being displaced.",
            E("H3", Polarity.Support, 0.4f),
            E("H2", Polarity.Support, 0.2f)));

        L.Add(S("C3b1", "D3",
            "An activity area remains uncleared after use.",
            E("H4", Polarity.Support, 0.5f),
            E("H1", Polarity.Counter, 0.4f)));

        L.Add(S("C3b2", "D3",
            "A nearby surface shows no signs of recent use.",
            E("H0", Polarity.Support, 0.2f),
            E("H2", Polarity.Counter, 0.2f)));

        // =========================================================
        // D4: Lobby / departure execution / monitoring
        // =========================================================
        L.Add(S("C4a1", "D4",
            "Items associated with going out remain present and unused.",
            E("H1", Polarity.Counter, 0.8f),
            E("H2", Polarity.Support, 0.4f),
            E("H4", Polarity.Support, 0.3f)));

        L.Add(S("C4a2", "D4",
            "Personal departure items are grouped together in one place.",
            E("H1", Polarity.Support, 0.5f)));

        L.Add(S("C4a3", "D4",
            "A departure-oriented preparation was not carried through.",
            E("H1", Polarity.Counter, 0.9f),
            E("H2", Polarity.Support, 0.5f),
            E("H4", Polarity.Support, 0.3f)));

        L.Add(S("C4b1", "D4",
            "A surveillance or monitoring system is non-functional.",
            E("H3", Polarity.Support, 0.7f),
            E("H2", Polarity.Support, 0.3f)));

        L.Add(S("C4b2", "D4",
            "No surveillance output is currently available.",
            E("H3", Polarity.Support, 0.6f)));

        L.Add(S("C4b3", "D4",
            "Monitoring visibility is impaired or obstructed.",
            E("H3", Polarity.Support, 0.5f)));

        L.Add(S("C4c1", "D4",
            "The entrance area preserves a normal, orderly arrangement.",
            E("H0", Polarity.Support, 0.3f),
            E("H2", Polarity.Counter, 0.3f)));

        L.Add(S("C4c2", "D4",
            "The entrance zone shows signs of local disturbance.",
            E("H2", Polarity.Support, 0.6f)));

        L.Add(S("C4c3", "D4",
            "Traces of foot traffic are visible near the entry area.",
            E("H2", Polarity.Support, 0.4f),
            E("H1", Polarity.Support, 0.2f)));

        // =========================================================
        // D5: Notes / planning / time anchor
        // =========================================================
        L.Add(S("C5a1", "D5",
            "A written reminder is present.",
            E("H1", Polarity.Support, 0.3f)));

        L.Add(S("C5a2", "D5",
            "The reminder is organized in a checklist-like format.",
            E("H1", Polarity.Support, 0.5f)));

        L.Add(S("C5a3", "D5",
            "The writing appears recent or actively used.",
            E("H1", Polarity.Support, 0.3f),
            E("H4", Polarity.Support, 0.2f)));

        L.Add(S("C5a4", "D5",
            "The written material reflects planned behavior.",
            E("H1", Polarity.Support, 0.7f)));

        L.Add(S("C5b1", "D5",
            "A timekeeping device is fixed at a specific time.",
            E("H2", Polarity.Support, 0.4f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C5b2", "D5",
            "The timekeeping mechanism is no longer running.",
            E("H2", Polarity.Support, 0.4f),
            E("H3", Polarity.Support, 0.4f)));

        L.Add(S("C5b3", "D5",
            "A fixed time point may serve as a temporal anchor for the event.",
            E("H2", Polarity.Support, 0.5f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C5c1", "D5",
            "A written schedule is documented on the page.",
            E("H1", Polarity.Support, 0.6f)));

        L.Add(S("C5c2", "D5",
            "The schedule reflects a structured routine.",
            E("H1", Polarity.Support, 0.5f),
            E("H0", Polarity.Support, 0.2f)));

        L.Add(S("C5c3", "D5",
            "A specific time-based plan is explicitly recorded.",
            E("H1", Polarity.Support, 0.8f)));

        // =========================================================
        // D6: Formal records / interruption / workstation
        // =========================================================
        L.Add(S("C6a1", "D6",
            "A formal document records physical condition information.",
            E("H0", Polarity.Support, 0.2f),
            E("H2", Polarity.Counter, 0.2f)));

        L.Add(S("C6a2", "D6",
            "The recorded physical state appears normal or stable.",
            E("H2", Polarity.Counter, 0.5f),
            E("H0", Polarity.Support, 0.3f)));

        L.Add(S("C6a3", "D6",
            "The document is presented in a formal record format.",
            E("H0", Polarity.Support, 0.2f)));

        L.Add(S("C6a4", "D6",
            "The record includes explicit time information.",
            E("H3", Polarity.Support, 0.2f),
            E("H2", Polarity.Support, 0.2f)));

        L.Add(S("C6b1", "D6",
            "A written communication was left unfinished.",
            E("H2", Polarity.Support, 0.6f),
            E("H1", Polarity.Counter, 0.4f)));

        L.Add(S("C6b2", "D6",
            "A task or written action stopped before completion.",
            E("H2", Polarity.Support, 0.6f),
            E("H1", Polarity.Counter, 0.4f)));

        L.Add(S("C6b3", "D6",
            "The pattern suggests interruption during an ongoing action.",
            E("H2", Polarity.Support, 0.8f),
            E("H1", Polarity.Counter, 0.5f)));

        L.Add(S("C6b4", "D6",
            "The writing reflects hesitation or uncertainty.",
            E("H1", Polarity.Support, 0.2f),
            E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C6c1", "D6",
            "A workstation device is inactive.",
            E("H0", Polarity.Support, 0.2f),
            E("H4", Polarity.Counter, 0.2f)));

        L.Add(S("C6c2", "D6",
            "The device is disconnected from power.",
            E("H1", Polarity.Support, 0.2f),
            E("H0", Polarity.Support, 0.2f)));

        L.Add(S("C6c3", "D6",
            "The workstation remains orderly and undisturbed.",
            E("H0", Polarity.Support, 0.3f),
            E("H2", Polarity.Counter, 0.2f)));

        // =========================================================
        // D7: Travel confirmation / indirect report
        // =========================================================
        L.Add(S("C7a1", "D7",
            "A confirmed departure plan is documented.",
            E("H1", Polarity.Support, 0.9f)));

        L.Add(S("C7a2", "D7",
            "The materials indicate concrete travel preparation.",
            E("H1", Polarity.Support, 0.8f)));

        L.Add(S("C7a3", "D7",
            "The available travel record shows no visible return arrangement.",
            E("H1", Polarity.Support, 0.5f),
            E("H3", Polarity.Support, 0.2f)));

        L.Add(S("C7b1", "D7",
            "Information is presented through a secondary report source.",
            E("H3", Polarity.Support, 0.2f)));

        L.Add(S("C7b2", "D7",
            "The report refers to an incident.",
            E("H0", Polarity.Counter, 0.2f),
            E("H2", Polarity.Support, 0.2f),
            E("H3", Polarity.Support, 0.2f)));

        L.Add(S("C7b3", "D7",
            "The report concerns a residential location.",
            E("H0", Polarity.Counter, 0.1f)));

        return L;
}
}
#endif


