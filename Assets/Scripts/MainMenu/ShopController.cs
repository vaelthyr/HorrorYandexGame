using System.Collections.Generic;
using UnityEngine;

public class ShopController : MonoBehaviour
{
    [SerializeField] private ShopCategory[] _categories;
    [SerializeField] private GameObject[] _categoriesButton;
    [SerializeField] private GameObject _categoriesContent;
    [SerializeField] private List<ShopButton> _shopButtons = new List<ShopButton>();
    [SerializeField] private ShopButton prefab;

    private SkinType _currentSkinType;
    private ObjectPool<ShopButton> _shopButtonsPool;

    private void Start()
    {
        if (prefab == null)
        {
            Debug.LogError("ShopController has no ShopButton prefab assigned.", this);
            return;
        }

        if (_categoriesContent == null)
        {
            Debug.LogError("ShopController has no categories content assigned.", this);
            return;
        }

        _shopButtonsPool = new ObjectPool<ShopButton>(prefab, 10, _categoriesContent.transform);
        ModifyContent(0);
    }

    public void ModifyContent(int id)
    {
        if (_shopButtonsPool == null)
        {
            return;
        }

        SkinType typeCategory;
        switch (id)
        {
            default:
            case 0:
                typeCategory = SkinType.Head;
                break;
            case 1:
                typeCategory = SkinType.Body;
                break;
            case 2:
                typeCategory = SkinType.Pants;
                break;
            case 3:
                typeCategory = SkinType.Shoes;
                break;
            case 4:
                typeCategory = SkinType.Sword;
                break;
            case 5:
                typeCategory = SkinType.Minion;
                break;
            case 6:
                typeCategory = SkinType.Dance;
                break;
        }

        if (_currentSkinType == typeCategory)
        {
            return;
        }

        _currentSkinType = typeCategory;

        foreach (ShopButton shopButton in _shopButtons)
        {
            _shopButtonsPool.ReturnObject(shopButton);
        }

        _shopButtons.Clear();

        for (int i = 0; i < _categories.Length; i++)
        {
            if (_categories[i].SkinType == typeCategory)
            {
                SwitchCategory(_categories[i].content);
                break;
            }
        }
    }

    private void SwitchCategory(SkinSO[] skins)
    {
        if (skins == null)
        {
            return;
        }

        for (int i = 0; i < skins.Length; i++)
        {
            SpawnButton(skins[i]);
        }
    }

    private void SpawnButton(SkinSO skin)
    {
        ShopButton button = _shopButtonsPool.GetObject();
        if (button == null)
        {
            return;
        }

        button.transform.SetParent(_categoriesContent.transform, false);
        _shopButtons.Add(button);
        button.ChangeContent(skin);
    }
}
