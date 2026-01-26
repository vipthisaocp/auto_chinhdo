namespace AutoEHT.Scripts;

/// <summary>
/// Script Phân Rã Stone trong Kho Thị Trấn
/// Mở kho → Tìm Stone1 → Phân rã liên tục hết → Tìm Stone2 → Phân rã liên tục hết → Lặp lại
/// </summary>
public class StoneFarmScript : GameScript
{
    public StoneFarmScript()
    {
        Id = "eht-002";
        Name = "Phân Rã Stone Farm";
        Description = "Kho Thị Trấn → Stone1 (phân rã hết) → Stone2 (phân rã hết)";
    }
    
    public override async Task Run()
    {
        while (!IsCancelled)
        {
            // === Bước 1: Mở Kho Thị Trấn ===
            await Wait(1500);
            if (!await WaitAndClick("kho_thi_tran", 15000, 2000))
            {
                Log("Không thấy Kho Thị Trấn, thử lại...");
                continue;
            }
            
            // === Bước 2: Tìm Stone1 và phân rã liên tục cho đến khi hết ===
            await DecomposeAll("stone1");
            
            // === Bước 3: Tìm Stone2 và phân rã liên tục cho đến khi hết ===
            await DecomposeAll("stone2");
            
            await Wait(2000);
            Log("Hoàn thành 1 vòng, tiếp tục...");
        }
    }
    
    /// <summary>Tìm item và phân rã liên tục cho đến khi không còn nút phân rã</summary>
    private async Task DecomposeAll(string itemTemplate)
    {
        Log($"🔧 Bắt đầu phân rã {itemTemplate}...");
        
        // Tìm item trước
        if (!await ScrollFindAndClick(itemTemplate, 800))
        {
            Log($"❌ Không tìm thấy {itemTemplate}");
            return;
        }
        
        // Phân rã liên tục cho đến khi hết
        int count = 0;
        while (!IsCancelled)
        {
            // Tìm nút phân rã
            if (await FindAndClick("phan_ra", 800))
            {
                count++;
                Log($"✅ Phân rã {itemTemplate} thành công (lần {count})");
                await Wait(500);
            }
            else
            {
                // Không thấy nút phân rã nữa - đã hết
                Log($"✔️ Hoàn thành phân rã {itemTemplate}: {count} lần");
                break;
            }
        }
    }
}
