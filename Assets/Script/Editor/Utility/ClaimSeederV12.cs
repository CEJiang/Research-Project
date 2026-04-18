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
        // --- A. 邊界與出入口 (Entry/Exit) ---
        "entry_point_unlocked",       // 門窗未上鎖
        "entry_point_forced_open",    // 門窗被外力撞開/撬開
        "perimeter_barrier_breached", // 圍欄、柵欄有缺口或被剪斷
        "internal_lock_engaged",      // 內部反鎖(如插銷、死鎖已上勾)

        // --- B. 結構與機械損害 (Structural Damage) ---
        "lock_cylinder_scratched",    // 鎖芯有工具刮痕
        "door_frame_deformed",        // 門框受壓變形
        "glass_shattered_inward",     // 玻璃碎片向內噴濺
        "surface_tool_indentation",   // 表面有工具壓痕(如撬棍痕)

        // --- C. 空間佈局與陳設 (Space & Arrangement) ---
        "furniture_overturned",       // 家具翻倒
        "carpet_displaced",           // 地毯被掀開或位移
        "wall_decor_tilted",          // 掛飾歪斜
        "drawer_pulled_out",          // 抽屜被拉開未關
        "general_room_disarray",      // 房間整體凌亂(翻找感)

        // --- D. 物品增減與搬運 (Inventory & Transit) ---
        "suitcase_staged_open",       // 行李箱打開擺放在易拿處
        "storage_hangers_empty",      // 衣櫃只剩空衣架
        "daily_items_absent",         // 常用生活品(牙刷、水杯)消失
        "foreign_trash_detected",     // 出現不屬於該處的垃圾或雜物
        "packing_material_present",   // 出現膠帶、紙箱、泡泡紙

        // --- E. 物理痕跡與生物遺留 (Physical Traces) ---
        "floor_drag_marks",           // 地面長條拖行痕跡
        "scuff_marks_high_friction",  // 鞋底急停、摩擦的黑色焦痕
        "dust_silhouette_missing",    // 灰塵中的空白處(顯示物品剛被拿走)
        "blood_spatter_detected",     // 血跡噴濺
        "struggle_fibers_detected",   // 撕扯留下的布料纖維

        // --- F. 束縛與限制 (Restraints) ---
        "adhesive_tape_residue",      // 膠帶殘膠或片段
        "ligature_furrow_material",   // 繩索、電線或束帶遺留
        "restraint_point_damage",     // 柱子或床腳有磨損(曾綁過東西)

        // --- G. 生活與時間指標 (Life & Temporal) ---
        "appliance_active_state",     // 電器運行中(電腦開著、電視有聲)
        "food_preparation_halted",    // 準備到一半的飲食
        "thermal_residual_heat",      // 餘溫(剛關掉的引擎、熱茶)
        "mail_pile_accumulated",      // 信件大量堆積未領

        // --- H. 技術與監控 (Tech & Surveillance) ---
        "camera_lens_occluded",       // 監控鏡頭被遮擋
        "cable_severed_physically",   // 線路被物理切斷
        "device_factory_reset",       // 設備被重置回原廠設定
        "storage_disk_missing",       // 硬碟、記憶卡被拔除

        // --- I. 文件與身份證件 (Documentation) ---
        "identity_doc_present",       // 護照/身分證留在原處
        "financial_tool_present",     // 錢包/信用卡留在原處
        "travel_itinerary_found",     // 發現旅遊行程表/機票
        "farewell_note_detected"      // 發現告別便條
    };

    private static readonly Dictionary<string, List<string>> featureClaimToRequiredFeatures = new()
    {
        // =========================================================
        // D1: Departure planning
        // =========================================================

        // C1a1: suitcases packed/closed or missing from storage
        { "C1a1", new() {
            "suitcase_staged_open" // 你現在的 vocab 比較偏「行李箱在外/可拿」；若你要「missing/closed」可再加新 feature
            // TODO (optional): "travel_container_missing_from_storage"
            // TODO (optional): "suitcase_closed_packed"
        }},

        // C1a2: wardrobes/drawers depleted or selectively emptied
        { "C1a2", new() {
            "storage_hangers_empty"
            // TODO (optional): "drawer_pulled_out" (若你把「抽屜被拉開+空」當成 depletion cue)
            // TODO (optional): "dust_silhouette_missing" (物品剛被拿走的灰塵輪廓)
        }},

        // C1a3: high daily-utility items absent
        { "C1a3", new() { "daily_items_absent" } },

        // C1a4: travel itinerary / booking confirmation / reservation email
        { "C1a4", new() { "travel_itinerary_found" } },

        // C1b1: destroyed physical records remnants
        { "C1b1", new() {
            // 你目前 vocab 沒有「shredded/charred records」類 feature
            // TODO: add feature
            "record_destruction_present"
        }},

        // C1b2: digital sanitization
        { "C1b2", new() { "device_factory_reset" } },

        // C1b3: communication disabled (router destroyed/cables disconnected)
        { "C1b3", new() { "cable_severed_physically" } }, // 先用這個當近似；若你要區分「拔線」vs「剪斷」可再加 feature

        // C1b4: surveillance interruption (media missing or deletion)
        { "C1b4", new() {
            "storage_disk_missing"
            // TODO (optional): "camera_lens_occluded" (如果你的 interruption 也包含遮鏡)
            // TODO: "surveillance_log_deleted" (若要精準對上 system logs deletion)
        }},

        // C1b5: voluntary note
        { "C1b5", new() { "farewell_note_detected" } },

        // C1c1: passport left behind
        { "C1c1", new() { "identity_doc_present" } },

        // C1c2: wallet/cash/cards left behind
        { "C1c2", new() { "financial_tool_present" } },

        // C1c3: vehicle keys left behind
        { "C1c3", new() {
            // 你目前 vocab 沒有「keys present」
            // TODO: add feature
            "keys_present"
        }},

        // C1c4: phone left behind
        { "C1c4", new() {
            // 你目前 vocab 沒有「phone present」
            // TODO: add feature
            "phone_present"
        }},

        // =========================================================
        // D2: Physical coercion
        // =========================================================

        // C2a1: lock tool marks / deformation
        { "C2a1", new() {
            "lock_cylinder_scratched",
            "surface_tool_indentation",
            "door_frame_deformed"
            // TODO (optional): "entry_point_forced_open"
        }},

        // C2a2: forced window entry (shattered glass / pried frame)
        { "C2a2", new() {
            "glass_shattered_inward"
            // TODO (optional): "door_frame_deformed" (若你用在窗框變形也可)
            // TODO: "window_frame_pried" (更精準)
        }},

        // C2a3: perimeter breach
        { "C2a3", new() { "perimeter_barrier_breached" } },

        // C2b1: indoor disturbance / displacement
        { "C2b1", new() {
            "general_room_disarray"
            // TODO (optional): "furniture_overturned"
            // TODO (optional): "wall_decor_tilted"
        }},

        // C2b2: directional movement / drag marks
        { "C2b2", new() { "floor_drag_marks" } },

        // C2b3: restraint materials present
        { "C2b3", new() {
            // 同樣「任一就算」語意，AND 會偏嚴格
            "adhesive_tape_residue",
            "ligature_furrow_material",
            "restraint_point_damage"
        }},

        // C2b4: chaotic tracks / soil disturbance at interaction point
        { "C2b4", new() {
            // 你目前 vocab 沒有「chaotic tracks」
            // TODO: add feature
            "chaotic_tracks_present"
        }},

        // -------- Counters / negations (這些不能只靠 requiredFeatures 表達) --------
        // C2c4: entry points intact / no tool marks
        { "C2c4", new() {
            // TODO: add a dedicated "entry_points_intact" feature (由規則檢查產生，不靠 LLM)
            "entry_points_intact" 
        }},

        // C2c5: no drag lines / no scuff patterns
        { "C2c5", new() {
            // TODO: add a dedicated "no_drag_marks_confirmed" feature (由規則檢查產生)
            "no_drag_marks_confirmed" 
        }},

        // C2c6: no continuous transfer trail toward exits
        { "C2c6", new() {
            // TODO: add "no_transfer_trail_confirmed" feature
            "no_transfer_trail_confirmed" 
        }},

        // C2c1: no foreign footprints beyond one set
        { "C2c1", new() {
            // TODO: add "no_foreign_tracks_confirmed" feature
            "no_foreign_tracks_confirmed" 
        }},

        // C2c2: no unfamiliar fibers/bio items
        { "C2c2", new() {
            // TODO: add "no_foreign_fibers_confirmed"
            "no_foreign_fibers_confirmed" 
        }},

        // C2c3: inventory consistent / no extraneous items
        { "C2c3", new() {
            // TODO: add "inventory_consistent_confirmed"
            "inventory_consistent_confirmed" 
        }},

        // =========================================================
        // D3: Access-control anomaly
        // =========================================================

        // C3a1: tamper seal disturbed
        { "C3a1", new() {
            // TODO: add "tamper_seal_broken"
            "tamper_seal_broken" 
        }},

        // C3a2: key management irregularity
        { "C3a2", new() {
            // TODO: add "key_management_irregularity"
            "key_management_irregularity" 
        }},

        // C3a3: recording media missing
        { "C3a3", new() { "storage_disk_missing" } },

        // C3a4: access log irregularity
        { "C3a4", new() {
            // TODO: add "access_log_irregularity"
            "access_log_irregularity" 
        }},

        // C3b1: cameras repositioned/occluded forming blind-spot path
        { "C3b1", new() { "camera_lens_occluded" } }, // 先用 occluded 當近似；repositioned 你可再加 feature

        // C3b2: sequential locks unlocked along route
        { "C3b2", new() {
            // TODO: add "unlock_route_observed"
            "unlock_route_observed" 
        }},

        // C3c1: system malfunction codes / low battery / maintenance warnings
        { "C3c1", new() {
            // TODO: add "system_malfunction_observed"
            "system_malfunction_observed" 
        }},

        // C3c2: distributed failures without pattern
        { "C3c2", new() {
            // TODO: add "distributed_failure_observed"
            "distributed_failure_observed" 
        }},

        // C3c3: failure due to natural wear/aging confirmed
        { "C3c3", new() {
            // TODO: add "natural_wear_confirmed"
            "natural_wear_confirmed" 
        }},

        // =========================================================
        // D4/D5/D6: Logistics & Baseline
        // =========================================================

        // C4a1: timestamp conflict
        { "C4a1", new() {
            // TODO: add "timestamp_conflict_observed"
            "timestamp_conflict_observed" 
        }},

        // C5a1: SOP terminology / work order formatting
        { "C5a1", new() {
            // TODO: add "sop_documentation_present"
            "sop_documentation_present" 
        }},

        // C6a1: cleaning apparatus active-use configuration
        { "C6a1", new() {
            // 你 vocab 沒有清潔推車/標示，建議新增
            // TODO: add "cleaning_apparatus_active_use"
            "cleaning_apparatus_active_use" 
        }},

        // C6a2: packing materials staged active-use
        { "C6a2", new() { "packing_material_present" } },

        // =========================================================
        // D7: On-premises presence
        // =========================================================

        // C7a1: recent activity cues (warm drink / active device / unfinished task)
        { "C7a1", new() {
            // 同樣是「任一即可」，AND 會偏嚴格；建議改成單一 aggregate feature
            "thermal_residual_heat",
            "appliance_active_state",
            "food_preparation_halted"
        }},

        // C7a2: unopened mail pile accumulated
        { "C7a2", new() { "mail_pile_accumulated" } },

        // C7b1: internally locked exit
        { "C7b1", new() { "internal_lock_engaged" } },

        // C7b4: trail ends inside (tracks/smears terminate at internal boundary)
        { "C7b4", new() {
            // TODO: add "trail_ends_inside"
            "trail_ends_inside" 
        }},

        // C7c1: clear exit trail to boundary exit point
        { "C7c1", new() {
            // TODO: add "clear_exit_trail"
            "clear_exit_trail" 
        }},

        // C7c2: vehicle activity at boundary (tire impressions / turnaround)
        { "C7c2", new() {
            // TODO: add "vehicle_activity_detected"
            "vehicle_activity_detected" 
        }},
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

        // Helper to reduce boilerplate
        ClaimEffect E(string hid, Polarity p, float w) => new ClaimEffect { hypothesisId = hid, polarity = p, weight = w };

        // ✅ Build a seed and auto-fill requiredFeatures from featureClaimToRequiredFeatures (atomic default).
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

        // ✅ For multi-feature AND claims (optional, future use)
        // ClaimSeed Sx(string id, string domainId, string description, IEnumerable<string> requiredFeatures, params ClaimEffect[] effects)
        // {
        //     return new ClaimSeed
        //     {
        //         id = id,
        //         domainId = domainId,
        //         description = description,
        //         requiredFeatures = requiredFeatures?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? new List<string>(),
        //         effects = effects.ToList()
        //     };
        // }

        // =========================================================
        // D1: Departure planning
        // =========================================================
        L.Add(S("C1a1", "D1",
            "Travel containers (suitcases/bags) are observed in a packed/closed state or are missing from their designated storage locations.",
            E("H1", Polarity.Support, 1.0f), E("H0", Polarity.Counter, 0.3f)));

        L.Add(S("C1a2", "D1",
            "Storage compartments (wardrobes/drawers) show a significant depletion of items or a pattern of selective emptying.",
            E("H1", Polarity.Support, 0.9f), E("H0", Polarity.Counter, 0.2f)));

        L.Add(S("C1a3", "D1",
            "Specific personal items of high daily utility are confirmed as absent from their typical on-premises locations.",
            E("H1", Polarity.Support, 0.8f)));

        L.Add(S("C1b1", "D1",
            "Remnants of destroyed physical records (shredded paper, charred fragments, or emptied binders) are present in disposal areas.",
            E("H1", Polarity.Support, 0.8f), E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C1b2", "D1",
            "Digital devices show evidence of data sanitization (factory resets, removed SIM/memory cards, or cleared local caches).",
            E("H1", Polarity.Support, 0.8f), E("H3", Polarity.Support, 0.3f)));

        L.Add(S("C1b3", "D1",
            "Communication hardware/software is rendered inactive (disconnected cables, disabled accounts, or physical destruction of routers).",
            E("H1", Polarity.Support, 0.7f)));

        L.Add(S("C1b4", "D1",
            "Surveillance recording media is absent or system logs show manual deletion/interruption during specific time windows.",
            E("H1", Polarity.Support, 0.5f), E("H3", Polarity.Support, 0.4f)));

        L.Add(S("C1b5", "D1",
            "A physically written or digital message is found stating a self-initiated and voluntary departure.",
            E("H1", Polarity.Support, 0.7f), E("H2", Polarity.Counter, 0.3f)));

        L.Add(S("C1c1", "D1",
            "Travel identification (passport) is observed to remain in its habitual location on the premises.",
            E("H1", Polarity.Counter, 0.9f), E("H4", Polarity.Support, 0.2f)));

        L.Add(S("C1c2", "D1",
            "Financial items (wallets/cash/bank cards) are observed to remain on the premises.",
            E("H1", Polarity.Counter, 0.8f)));

        L.Add(S("C1c3", "D1",
            "Transportation control items (vehicle keys) remain in their standard on-premises storage.",
            E("H1", Polarity.Counter, 0.7f)));

        L.Add(S("C1c4", "D1",
            "Primary personal mobile device is observed to remain on the premises.",
            E("H1", Polarity.Counter, 0.8f), E("H4", Polarity.Support, 0.2f)));

        // ✅ NEW: Travel Email / Itinerary (你提到的 Travel Email)
        L.Add(S("C1a4", "D1",
            "Concrete travel planning artifacts are present (itinerary, booking confirmation, printed tickets, reservation emails).",
            E("H1", Polarity.Support, 0.8f), E("H0", Polarity.Counter, 0.2f)));

        // =========================================================
        // D2: Physical coercion
        // =========================================================
        L.Add(S("C2a1", "D2",
            "Mechanical deformation or tool marks are visible on door lock assemblies (pried plates, sheared pins, or broken cylinders).",
            E("H2", Polarity.Support, 1.0f), E("H0", Polarity.Counter, 0.5f)));

        L.Add(S("C2a2", "D2",
            "Forced structural failure is visible on window frames or latches (shattered glass near latches or pried frames).",
            E("H2", Polarity.Support, 0.9f), E("H0", Polarity.Counter, 0.4f)));

        L.Add(S("C2a3", "D2",
            "Perimeter barriers show signs of forceful breaching (bent bars, cut fencing wires, or compromised gate hinges).",
            E("H2", Polarity.Support, 0.8f)));

        L.Add(S("C2b1", "D2",
            "Interior environment shows signs of sudden displacement (overturned furniture, shattered glass on the floor, or scattered household items).",
            E("H2", Polarity.Support, 0.9f), E("H0", Polarity.Counter, 0.3f)));

        L.Add(S("C2b2", "D2",
            "Linear trajectories of surface displacement (drag lines, elongated smears, or displaced floor coverings) are observed on floor surfaces.",
            E("H2", Polarity.Support, 1.0f)));

        L.Add(S("C2b3", "D2",
            "Materials associated with restraint (adhesive tape fragments, severed ropes, or plastic zip-tie residues) are present.",
            E("H2", Polarity.Support, 0.8f)));

        L.Add(S("C2b4", "D2",
            "Exterior ground surfaces show chaotic/overlapping track patterns and soil disturbance near a specific interaction point.",
            E("H2", Polarity.Support, 0.7f)));

        L.Add(S("C2c4", "D2",
            "Checked(scope=...): Alternative entry points (windows/secondary doors/gates) are intact with no tool marks, frame distortion, or latch damage detected.",
            E("H2", Polarity.Counter, 0.8f), E("H3", Polarity.Support, 0.4f), E("H0", Polarity.Support, 0.2f)));

        L.Add(S("C2c5", "D2",
            "Checked(scope=...): No drag lines, floor abrasion, or directional scuff patterns consistent with forced movement are observed.",
            E("H2", Polarity.Counter, 0.7f), E("H0", Polarity.Support, 0.2f)));

        L.Add(S("C2c6", "D2",
            "Checked(scope=...): No continuous transfer trail (blood/soil/fibers) extends from interior deposits toward any exit points.",
            E("H2", Polarity.Counter, 0.7f), E("H4", Polarity.Support, 0.2f)));

        L.Add(S("C2c1", "D2",
            "Checked(scope=...): No foreign tread patterns/footprints are detected beyond a single set of familiar tracks.",
            E("H2", Polarity.Counter, 0.9f), E("H0", Polarity.Support, 0.2f)));

        L.Add(S("C2c2", "D2",
            "Checked(scope=...): No unfamiliar textile fibers or biological items are found on inspected surfaces/furniture.",
            E("H2", Polarity.Counter, 0.6f)));

        L.Add(S("C2c3", "D2",
            "Checked(scope=...): Physical inventory count matches the verified baseline with no extraneous items introduced.",
            E("H2", Polarity.Counter, 0.4f)));

        // =========================================================
        // D3: Access-control anomaly
        // =========================================================
        L.Add(S("C3a1", "D3",
            "Security seals or tamper-evident stickers on restricted hardware show visible fractures or adhesive disturbance.",
            E("H3", Polarity.Support, 0.6f), E("H2", Polarity.Support, 0.3f)));

        L.Add(S("C3a2", "D3",
            "Key management systems show irregularities (key cabinet opened without authorization, missing master keys, or altered logs).",
            E("H3", Polarity.Support, 0.8f)));

        L.Add(S("C3a3", "D3",
            "Physical recording media (hard drives/tapes) are absent from their designated storage slots within the surveillance unit.",
            E("H3", Polarity.Support, 0.6f), E("H2", Polarity.Support, 0.5f), E("H1", Polarity.Support, 0.3f)));

        L.Add(S("C3a4", "D3",
            "Electronic access databases show irregular gaps, overwritten timestamps, or recent administrative clearing.",
            E("H3", Polarity.Support, 0.6f), E("H1", Polarity.Support, 0.3f)));

        L.Add(S("C3b1", "D3",
            "Multiple optical sensors (cameras) are physically repositioned or occluded in a sequence that creates a blind-spot path.",
            E("H2", Polarity.Support, 0.7f), E("H3", Polarity.Support, 0.7f)));

        L.Add(S("C3b2", "D3",
            "Sequential locking mechanisms are observed in an unlocked state consistent with a directional movement route.",
            E("H2", Polarity.Support, 0.6f), E("H3", Polarity.Support, 0.6f)));

        L.Add(S("C3c1", "D3",
            "System interfaces display active malfunction codes, low-battery alerts, or maintenance-required warnings.",
            E("H0", Polarity.Support, 0.8f), E("H2", Polarity.Counter, 0.4f), E("H3", Polarity.Counter, 0.4f)));

        L.Add(S("C3c2", "D3",
            "Technical failures are distributed across unrelated hardware nodes without any directional or logical grouping.",
            E("H0", Polarity.Support, 0.6f)));

        L.Add(S("C3c3", "D3",
            "Checked(scope=...): Physical inspection confirms device failure is due to natural oxidation, wear, or component aging.",
            E("H0", Polarity.Support, 0.7f), E("H2", Polarity.Counter, 0.3f), E("H3", Polarity.Counter, 0.3f)));

        // =========================================================
        // D4/D5/D6: Logistics & Baseline
        // =========================================================
        L.Add(S("C4a1", "D4",
            "Temporal records show conflicting data (manually written dates conflict with system-generated timestamps).",
            E("H3", Polarity.Support, 0.7f), E("H0", Polarity.Counter, 0.2f)));

        L.Add(S("C5a1", "D5",
            "Documentation present uses specialized SOP terminology, work-order formatting, or phased project language.",
            E("H0", Polarity.Support, 0.5f)));

        L.Add(S("C6a1", "D6",
            "Cleaning apparatus (carts/chemicals) are arranged in an active-use configuration with safety signage deployed.",
            E("H0", Polarity.Support, 0.9f)));

        L.Add(S("C6a2", "D6",
            "Packing materials (boxes/tape/bubble wrap) are staged in an active-use arrangement within the primary work zone.",
            E("H0", Polarity.Support, 0.8f)));

        // =========================================================
        // D7: On-premises presence
        // =========================================================
        L.Add(S("C7a1", "D7",
            "Recent activity cues suggest very recent presence (warm drink, active device, unfinished task state).",
            E("H4", Polarity.Support, 0.8f), E("H1", Polarity.Counter, 0.4f)));
            
        L.Add(S("C7a2", "D7",
            "A pile of unopened mail/envelopes is present (sealed, unbroken, and visibly accumulated).",
            E("H4", Polarity.Support, 0.6f),  // still inside / recent daily life indicator
            E("H1", Polarity.Counter, 0.2f)   // leaving voluntarily is less consistent
        ));

        L.Add(S("C7b1", "D7",
            "An entry/exit point is observed to be secured via an internal-only locking mechanism (manual deadbolt/latch).",
            E("H4", Polarity.Support, 1.0f)));

        L.Add(S("C7b4", "D7",
            "A directional movement trail (tracks/smears) terminates at an internal boundary with no detectable exit trajectory.",
            E("H4", Polarity.Support, 0.6f)));

        L.Add(S("C7c1", "D7",
            "A continuous sequence of directional tracks forms a direct path from an interior zone to a boundary exit point.",
            E("H4", Polarity.Counter, 0.7f), E("H2", Polarity.Support, 0.3f)));

        L.Add(S("C7c2", "D7",
            "Exterior boundary points show tire impressions or ground disturbances consistent with a vehicle stationary/turnaround event.",
            E("H4", Polarity.Counter, 0.8f), E("H1", Polarity.Support, 0.3f), E("H2", Polarity.Support, 0.3f)));

        return L;
    }
}
#endif


