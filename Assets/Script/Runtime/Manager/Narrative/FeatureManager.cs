using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FeatureManager : Singleton<FeatureManager>
{
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

    public string GetFeature()
    {
        string featureText = "";
        foreach (var feature in features)
        {
            featureText += $"- Feature: {feature}\n";
        }
        return featureText;
    }

    public async Task<List<FeatureSelectionResult>> EvaluateEvidenceFeaturesAsync(EvidenceModel evidenceModel)
    {
        var featureSelectionResults = await FeatureSelectionManager.Instance.GenerateFeatureSelection(evidenceModel);

        return featureSelectionResults;
    }
}
