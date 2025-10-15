"""
UIRule file management tool
ManageUICreate rule file，Including creation、Modify、Delete and get rules
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_ui_rule_manage_tools(mcp: FastMCP):
    @mcp.tool("ui_rule_manage")
    def ui_rule_manage(
        ctx: Context,
        action: str = Field(
            ...,
            title="Operation type",
            description="Operation type",
            examples=["create", "modify", "delete", "get", "list", "apply", "validate"]
        ),
        rule_name: str = Field(
            ...,
            title="Rule name",
            description="Rule name",
            examples=["ButtonRule", "PanelRule", "TextRule"]
        ),
        rule_type: Optional[str] = Field(
            None,
            title="Rule type",
            description="Rule type",
            examples=["Button", "Panel", "Text", "Image", "InputField", "ScrollView", "Dropdown", "Toggle", "Slider", "Scrollbar"]
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="Rule properties",
            description="Rule properties dictionary",
            examples=[{"width": 200, "height": 50, "color": [1, 0, 0, 1]}]
        ),
        constraints: Optional[Dict[str, Any]] = Field(
            None,
            title="Constraints",
            description="Constraints dictionary",
            examples=[{"min_width": 100, "max_width": 500, "required_components": ["Button", "Text"]}]
        ),
        validation_rules: Optional[List[str]] = Field(
            None,
            title="Validate rules",
            description="Validation rules list",
            examples=[["width > 0", "height > 0", "has_button_component"]]
        ),
        target_object: Optional[str] = Field(
            None,
            title="Target object",
            description="Target object name or path",
            examples=["Canvas/UI/Button", "Button", "Panel"]
        ),
        force_apply: Optional[bool] = Field(
            False,
            title="Force apply",
            description="Whether to force apply rules"
        ),
        validate_only: Optional[bool] = Field(
            False,
            title="Validate only",
            description="Validate only without applying"
        ),
        create_missing: Optional[bool] = Field(
            False,
            title="Create missing",
            description="Whether to create missing components"
        ),
        remove_extra: Optional[bool] = Field(
            False,
            title="Remove redundancies",
            description="Whether to remove extra components"
        ),
        preserve_hierarchy: Optional[bool] = Field(
            True,
            title="Keep hierarchy",
            description="Whether to keep object hierarchy"
        ),
        backup_before_apply: Optional[bool] = Field(
            True,
            title="Backup before applying",
            description="Whether to back up objects before applying"
        ),
        log_changes: Optional[bool] = Field(
            True,
            title="Record changes",
            description="Whether to log changes"
        ),
        rule_file_path: Optional[str] = Field(
            None,
            title="Rule file path",
            description="Rule file path",
            examples=["Assets/UI/Rules/ButtonRule.json", "Assets/Config/UI/"]
        ),
        export_format: Optional[str] = Field(
            None,
            title="Export format",
            description="Export format",
            examples=["json", "xml", "yaml"]
        ),
        import_format: Optional[str] = Field(
            None,
            title="Import format",
            description="Import format",
            examples=["json", "xml", "yaml"]
        ),
        template_name: Optional[str] = Field(
            None,
            title="Template name",
            description="Template name",
            examples=["StandardButton", "ModernPanel", "ClassicText"]
        ),
        category: Optional[str] = Field(
            None,
            title="Rule category",
            description="Rule category",
            examples=["UI", "Gameplay", "System", "Custom"]
        ),
        tags: Optional[List[str]] = Field(
            None,
            title="Tags",
            description="Tag list",
            examples=[["button", "ui", "interactive"], ["panel", "container", "layout"]]
        ),
        description: Optional[str] = Field(
            None,
            title="Rule description",
            description="Rule description",
            examples=["Standard button rule", "Panel layout rule", "Text display rule"]
        ),
        version: Optional[str] = Field(
            None,
            title="Rule version",
            description="Rule version",
            examples=["1.0", "2.1", "3.0.1"]
        ),
        author: Optional[str] = Field(
            None,
            title="Rule author",
            description="Rule author",
            examples=["Designer", "Developer", "Artist"]
        ),
        created_date: Optional[str] = Field(
            None,
            title="Created date",
            description="Created date",
            examples=["2024-01-01", "2024-12-25"]
        ),
        modified_date: Optional[str] = Field(
            None,
            title="Modified date",
            description="Modified date",
            examples=["2024-01-01", "2024-12-25"]
        ),
        is_active: Optional[bool] = Field(
            True,
            title="Whether active",
            description="Whether the rule is active"
        ),
        priority: Optional[int] = Field(
            0,
            title="Priority",
            description="Rule priority",
            examples=[0, 1, 5, 10]
        ),
        dependencies: Optional[List[str]] = Field(
            None,
            title="Dependent rules",
            description="Dependent rules list",
            examples=[["BaseUIRule", "ColorRule"], ["LayoutRule"]]
        ),
        conflicts: Optional[List[str]] = Field(
            None,
            title="Conflicting rules",
            description="Conflicting rules list",
            examples=[["OldButtonRule", "LegacyPanelRule"]]
        ),
        conditions: Optional[Dict[str, Any]] = Field(
            None,
            title="Apply conditions",
            description="Apply-conditions dictionary",
            examples=[{"platform": "mobile", "resolution": "high", "theme": "dark"}]
        ),
        effects: Optional[Dict[str, Any]] = Field(
            None,
            title="Apply effects",
            description="Apply-effects dictionary",
            examples=[{"animation": "fade", "sound": "click", "haptic": "light"}]
        ),
        metadata: Optional[Dict[str, Any]] = Field(
            None,
            title="Metadata",
            description="Metadata dictionary",
            examples=[{"project": "MyGame", "team": "UI", "status": "approved"}]
        )
    ) -> Dict[str, Any]:
        """
        UIRule file management tool（Secondary tool）
        
        Supported operations:
        - create: Create new rule
        - modify: Modify existing rule
        - delete: Delete rule
        - get: Get rule info
        - list: List all rules
        - apply: Apply rules to object
        - validate: Validate rules
        """
        return get_common_call_response("ui_rule_manage")
