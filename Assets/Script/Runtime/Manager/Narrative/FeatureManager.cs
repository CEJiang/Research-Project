using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FeatureManager : Singleton<FeatureManager>
{
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
