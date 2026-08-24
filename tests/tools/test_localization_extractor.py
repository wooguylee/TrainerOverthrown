import importlib.util
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / "tools" / "extract_unity_localization.py"


def load_extractor_module():
    spec = importlib.util.spec_from_file_location("extract_unity_localization", MODULE_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class LocalizationExtractorTests(unittest.TestCase):
    def test_joins_shared_keys_and_sorts_stable_ids(self):
        extractor = load_extractor_module()
        shared_tables = [
            {
                "m_Name": "UI Shared Data",
                "m_Entries": [
                    {"m_Id": 20, "m_Key": "MENU_Settings"},
                    {"m_Id": 10, "m_Key": "MENU_NewWorld"},
                ],
            }
        ]
        english_tables = [
            {
                "m_Name": "UI_en",
                "m_TableData": [
                    {"m_Id": 20, "m_Localized": "Settings"},
                    {"m_Id": 10, "m_Localized": "New world"},
                ],
            }
        ]

        entries = extractor.join_tables(shared_tables, english_tables)

        self.assertEqual(
            ["UI/MENU_NewWorld", "UI/MENU_Settings"],
            [entry["id"] for entry in entries],
        )
        self.assertEqual("New world", entries[0]["source"])
        self.assertEqual("UI", entries[0]["table"])
        self.assertEqual(10, entries[0]["keyId"])

    def test_rejects_duplicate_stable_id(self):
        extractor = load_extractor_module()
        shared_tables = [
            {
                "m_Name": "UI Shared Data",
                "m_Entries": [
                    {"m_Id": 10, "m_Key": "MENU_NewWorld"},
                    {"m_Id": 20, "m_Key": "MENU_NewWorld"},
                ],
            }
        ]
        english_tables = [
            {
                "m_Name": "UI_en",
                "m_TableData": [
                    {"m_Id": 10, "m_Localized": "New world"},
                    {"m_Id": 20, "m_Localized": "Another world"},
                ],
            }
        ]

        with self.assertRaisesRegex(ValueError, "duplicate stable id"):
            extractor.join_tables(shared_tables, english_tables)


if __name__ == "__main__":
    unittest.main()
