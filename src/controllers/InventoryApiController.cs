using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace ValheimStreamerApi
{
    public class InventoryApiController : ApiController<InventoryApiController.ActionMainData>
    {
        public class ActionMainData
        {
            [JsonProperty("playerName")] public string playerName { get; set; }
        }

        private class ActionDisarmamentData
        {
            [JsonProperty("playerName")] public string playerName { get; set; }
        }

        private class Item
        {
            [JsonProperty("name")]        public string name        { get; set; }
            [JsonProperty("prefabName")]  public string prefabName  { get; set; }
            [JsonProperty("stack")]       public int    stack       { get; set; }
            [JsonProperty("quality")]     public int    quality     { get; set; }
            [JsonProperty("durability")]  public float  durability  { get; set; }
            [JsonProperty("isEquipable")] public bool   isEquipable { get; set; }
        }

        private class RpcResponseData
        {
            [JsonProperty("items")] public List<Item> items { get; set; }
            [JsonProperty("count")] public int        count { get; set; }
        }

        private class RpcStatusData
        {
            [JsonProperty("status")] public string status { get; set; }
        }

        public InventoryApiController()
        {
            http = "/api/inventory";
            rpc  = "ValheimStreamerApi/api/inventory";

            RegisterHttpAction<ActionDisarmamentData>("disarmament", ActionDisarmament);

            RegisterRpcAction<object>("get-inventory", GetInventory);
            RegisterRpcAction<object>("disarmament", Disarmament);
        }

        // === Server (HTTP) ===

        protected override async Task<object> Action(ActionMainData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "get-inventory", new {});
            return JsonParser.Parse<RpcResponseData>(zData);
        }

        private async Task<object> ActionDisarmament(ActionDisarmamentData data)
        {
            var targetPeer = RpcManager.FindPlayerByName(data.playerName);
            if (targetPeer == null) return new { error = "no player peer" };

            var zData = await RpcManager.SendMessageAsync(rpc, targetPeer.m_uid, "disarmament", new {});
            return JsonParser.Parse<RpcStatusData>(zData);
        }

        // === Client (RPC) ===

        private object GetInventory(object _data)
        {
            Player player = Player.m_localPlayer;
            if (player == null) return new { status = "not a player" };

            Inventory inventory = player.GetInventory();
            if (inventory == null) return new { status = "not a player inventory" };

            List<ItemDrop.ItemData> items = inventory.GetAllItems();

            var itemsDto = items.Select(item => new Item
            {
                name        = item.m_shared.m_name,
                prefabName  = "",
                stack       = item.m_stack,
                quality     = item.m_quality,
                durability  = item.m_durability,
                isEquipable = item.IsEquipable()
            }).ToList();

            return new RpcResponseData
            {
                items = itemsDto,
                count = items.Count
            };
        }

        private object Disarmament(object _data)
        {
            Player player = Player.m_localPlayer;
            Inventory inventory = player.GetInventory();

            var weapons = inventory.GetAllItems()
                .Where(item => item.m_equipped && (
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs ||
                    item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder
                )).ToList();

            if (weapons.Count == 0)
                return new { status = "no weapons" };

            Vector3 center = player.transform.position;
            float radius = 3f;

            for (int i = 0; i < weapons.Count; i++)
            {
                var item = weapons[i];
                player.UnequipItem(item, triggerEquipEffects: false);

                float angle = 2 * Mathf.PI * i / weapons.Count;
                Vector3 position = new Vector3(
                    center.x + radius * Mathf.Cos(angle),
                    center.y + 1f,
                    center.z + radius * Mathf.Sin(angle)
                );

                if (item.m_dropPrefab != null)
                {
                    GameObject go = GameObject.Instantiate(item.m_dropPrefab, position, Quaternion.identity);
                    ItemDrop drop = go.GetComponent<ItemDrop>();
                    if (drop != null)
                    {
                        drop.m_itemData = item.Clone();

                        Rigidbody rb = go.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            Vector3 outward = (position - center).normalized;
                            rb.linearVelocity = outward * 4f + Vector3.up * 3f;
                        }
                    }
                }

                inventory.RemoveItem(item);
            }

            return new { status = "ok" };
        }
    }
}
