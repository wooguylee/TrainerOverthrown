namespace VVooOverthrown.App.ViewModels;

public sealed record TrainerResourceOption(int Value, string Label);

public static class TrainerResourceOptions
{
    public static IReadOnlyList<TrainerResourceOption> All { get; } =
    [
        new(1, "통나무 (Logs)"),
        new(2, "돌 (Rocks)"),
        new(3, "화산 식물 (VolcanicPlants)"),
        new(4, "약초 (Herbs)"),
        new(5, "채집물 (Forage)"),
        new(6, "섬유 (Fiber)"),
        new(7, "유기 폐기물 (OrganicWaste)"),
        new(8, "고철 (Scrap)"),
        new(10, "과일 (Fruit)"),
        new(11, "채소 (Vegetables)"),
        new(12, "밀 (Wheat)"),
        new(13, "생선 (RawFish)"),
        new(14, "사체 (Carcasses)"),
        new(15, "판자 (Planks)"),
        new(16, "벽돌 (Bricks)"),
        new(17, "부품 (Components)"),
        new(18, "코인 (Coin)"),
        new(19, "연료 (Fuel)"),
        new(20, "의약품 (Medicine)"),
        new(21, "화약 (Gunpowder)"),
        new(22, "마나 (Mana)"),
        new(23, "비료 (Fertilizer)"),
        new(24, "사료 (Fodder)"),
        new(25, "곡물 (Grain)"),
        new(27, "광석 (Ore)"),
        new(28, "황 (Sulfur)"),
        new(29, "직물 (Cloth)"),
        new(30, "음료 (Drink)"),
        new(31, "비누 (Soap)"),
        new(32, "패션 (Fashion)"),
        new(33, "오락 (Entertainment)"),
        new(34, "스튜 (Stew)"),
        new(35, "잼 (Jam)"),
        new(36, "빵 (Bread)"),
        new(37, "생선튀김 (FriedFish)"),
        new(38, "소시지 (Sausages)"),
    ];
}
