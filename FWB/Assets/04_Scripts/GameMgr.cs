using System.Linq;
using UnityEngine;
using static BluePrintTable;
using static WeaponDataTable;

/// <summary>
/// 게임 중 사용되는 데이터를 관리하는 매니저 컴포넌트
/// </summary>
public class GameMgr : SingletonMono<GameMgr>
{
    public bool initDone;
    public Day day = Day.월;
    public int week = 1;
    public int credit;
    public int dayChipsetCost;
    public int dayBonusRevenue;
    public int dayRentCost;
    public int dayRevenue;
    public int dayCustomerCnt;
    public int dayRenom;
    public int dayTendency;
    public int tendency;
    public WeaponDataTable weaponDataTable;
    public OrderTable orderTable;
    public ChipTable chipTable;
    public AbilityTable abilityTable;
    // public ConditionTable requestTable;
    public WeaponDataTable.BluePrint currentBluePrint;

    public enum Day
    {
        월 = 1,
        화 = 2,
        수 = 3,
        목 = 4,
        금 = 5,
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        PlayerPrefs.DeleteAll();

        foreach (var category in weaponDataTable.bluePrintCategoryList)
        {
            foreach (var bp in category.bluePrintList)
            {
                bool enable = string.IsNullOrEmpty(bp.howToGet);
                bp.orderEnable = enable;
                bp.createEnable = enable;
            }
        }

        foreach (var order in orderTable.orderList)
        {
            order.orderEnable = string.IsNullOrEmpty(order.orderCondition);
        }

        foreach (var chip in chipTable.chipList)
        {
            bool enable = string.IsNullOrEmpty(chip.howToGet);
            chip.createEnable = enable;
        }

        // TODO: Ability와 Order desc 분리 후 하드코드 제거
        var durability = GetAbility("a_durability");
        durability.orderEnable = false;

        // TODO: Order 데이터테이블의 enable 초기화
    }

    /// <summary>
    /// 하루가 지났을 때에 리셋해야하는 정보들을 리셋함
    /// </summary>
    public void ResetDayData()
    {
        dayChipsetCost = 0;
        dayBonusRevenue = 0;
        dayRentCost = 0;
        dayRevenue = 0;
        dayCustomerCnt = 0;
        dayRenom = 0;
        dayTendency = 0;
    }

    /// <summary>
    /// 하루가 지났을 때에 변경해줘야 하는 정보들을 세팅함
    /// </summary>
    public void SetNextDayData()
    {
        day++;
        if ((int)day > 5)
        {
            week++;
            day = (Day)1;
        }

        tendency += dayTendency;
    }

    /// <summary>
    /// 키 값에 맞는 무기카테고리정보를 반환
    /// </summary>
    public BluePrintCategory GetWeaponCategory(string categoryKey)
    {
        return weaponDataTable.bluePrintCategoryList.Find(x => x.categoryKey.Equals(categoryKey));
    }

    /// <summary>
    /// 키 값에 맞는 무기정보를 반환 (카테고리키를 알고있는 경우, 이 함수를 사용할 것)
    /// </summary>
    public WeaponDataTable.BluePrint GetWeapon(string categoryKey, string key)
    {
        var category = GetWeaponCategory(categoryKey);
        return category.bluePrintList.Find(x => x.bluePrintKey.Equals(key));
    }

    /// <summary>
    /// 키 값에 맞는 무기정보를 반환
    /// </summary>
    public WeaponDataTable.BluePrint GetWeapon(string key)
    {
        foreach (var category in weaponDataTable.bluePrintCategoryList)
        {
            foreach (var bp in category.bluePrintList)
            {
                if (bp.bluePrintKey.Equals(key))
                {
                    return bp;
                }
            }
        }
        return null;
    }

    public OrderTable.Order GetOrder(string orderKey)
    {
        var targetOrder = orderTable.orderList.Find(x => x.orderKey.Equals(orderKey));
        if (targetOrder == null)
        {
            Debug.Log("orderKey에 해당하는 Order가 없습니다.");
            return null;
        }

        return targetOrder;
    }

    public OrderTable.Order GetRandomNewOrder(string exceptionKey)
    {
        var orderableOrderList = orderTable.orderList.FindAll(x =>
            x.orderEnable && !x.orderKey.Equals(exceptionKey)).ToList();
        var index = UnityEngine.Random.Range(0, orderableOrderList.Count);
        return orderTable.GetNewOrder(orderableOrderList[index]);
    }

    /// <summary>
    /// 키 값에 맞는 칩 정보를 반환
    /// </summary>
    public ChipTable.Chip GetChip(string chipKey)
    {
        return chipTable.chipList.Find(x => x.chipKey.Equals(chipKey));
    }

    /// <summary>
    /// 키 값에 맞는 능력치 정보를 반환
    /// </summary>
    public AbilityTable.Ability GetAbility(string abilityKey)
    {
        return abilityTable.abilityList.Find(x => x.abilityKey.Equals(abilityKey));
    }
}
