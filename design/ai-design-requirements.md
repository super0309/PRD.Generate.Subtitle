# Yêu cầu thiết kế UI cho AI Design Tool

## 📋 Trả lời các câu hỏi thiết kế

### 1. Phong cách thiết kế và Dark Mode
**Trả lời**: 
- ✅ **Light Mode là ưu tiên chính** (default)
- ✅ **Light mode** với background trắng (#FFFFFF) hoặc light gray (#FAFAFA)
- ⚠️ Dark mode là optional (có thể bổ sung sau)
- **Lý do**: 
  - Ứng dụng Windows Desktop chuyên nghiệp
  - Dễ đọc thông tin trong môi trường văn phòng
  - Phù hợp với hệ thống design của Windows 11 (Fluent Design)
  
**Style Direction**:
- Modern, clean, minimalist
- Flat design với subtle shadows
- Rounded corners (4px for buttons, 8px for progress bars)
- Microsoft Fluent Design inspiration

---

### 2. Action Menu hiển thị
**Trả lời**: 
- ✅ **Menu ẩn, chỉ hiển thị khi click vào icon [⋮]**
- Icon [⋮] (vertical ellipsis) luôn visible ở cột cuối cùng
- Khi click → Dropdown context menu xuất hiện
- Menu options:
  ```
  📄 View Details
  📁 Open Output Folder (enabled nếu completed)
  📋 Open Subtitle File (enabled nếu completed)
  ────────────
  🔄 Retry (enabled nếu failed)
  🚫 Cancel (enabled nếu đang processing)
  ────────────
  🗑️ Remove
  ```
- **Position**: Menu xuất hiện bên dưới icon [⋮], align right
- **Behavior**: Click outside để đóng menu

---

### 3. Icons cho trạng thái file
**Trả lời**: 
- ✅ **Sử dụng CÙNG LÚC cả icon VÀ màu sắc**
- **Lý do**: Accessibility + Visual clarity

**Status Indicators**:

#### Status Column:
```
┌─────────────────────────────┐
│ ● Queued                    │  ← Gray circle + Gray text
│ ● Converting                │  ← Blue circle (pulsing) + Blue text
│ ● Transcribing              │  ← Purple circle (pulsing) + Purple text
│ ✓ Completed                 │  ← Green checkmark + Green text
│ ✕ Failed                    │  ← Red X + Red text
│ ⏸ Paused                    │  ← Orange pause icon + Orange text
└─────────────────────────────┘
```

#### Icon Specifications:
- **Queued**: ○ Empty circle, Gray (#757575)
- **Converting**: ● Filled circle with pulse animation, Blue (#2196F3)
- **Transcribing**: ● Filled circle with pulse animation, Purple (#9C27B0)
- **Completed**: ✓ Checkmark, Green (#4CAF50)
- **Failed**: ✕ X mark, Red (#F44336)
- **Paused**: ⏸ Pause icon, Orange (#FF9800)

#### Icon Size: 
- Status icons: 16x16px
- File type icon: 24x24px
- Toolbar icons: 20x20px

#### Icon Style:
- Material Design Icons hoặc Fluent UI System Icons
- Line style, 2px stroke
- Rounded edges

---

### 4. Responsive Design
**Trả lời**: 
- ✅ **Có responsive, nhưng không phải mobile**
- Target: **Desktop only** (Windows 10/11)

**Responsive Requirements**:

#### Minimum Size:
```
Width: 900px (không được nhỏ hơn)
Height: 600px (không được nhỏ hơn)
```

#### Optimal Size:
```
Width: 1200px
Height: 800px
```

#### Maximum Size:
```
Width: 1920px (full HD)
Height: 1080px
```

#### Behavior khi resize:

**Width adjustment**:
- < 1000px: 
  - Output path TextBox thu ngắn
  - Some button text → icon only
  - DataGrid columns adjust proportionally
  
- 1000px - 1400px:
  - Normal layout
  - All elements visible with full labels
  
- > 1400px:
  - Extra spacing
  - Wider columns
  - More log entries visible

**Height adjustment**:
- < 700px:
  - Log viewer: 100px height
  - DataGrid: 3-4 rows visible
  - Scroll required
  
- 700px - 900px:
  - Log viewer: 200px height
  - DataGrid: 6-8 rows visible
  - Comfortable viewing
  
- > 900px:
  - Log viewer: 250-300px height
  - DataGrid: 10+ rows visible
  - Spacious layout

#### Grid Row Sizing:
```
Row 0 (Toolbar): Fixed 50px
Row 1 (Job List): Star (*) - Flexible
Row 2 (Progress): Fixed 120px
Row 3 (Log): Fixed 200px (can resize with splitter)
```

#### Optional: Splitter between sections
- Allow user to resize Log Viewer height
- Min: 100px, Max: 400px

---

### 5. Công nghệ & Framework
**Trả lời**: 
- ✅ **WPF (Windows Presentation Foundation)**
- Platform: **.NET 6 hoặc .NET 7**
- Language: **C# + XAML**

**Technical Stack**:
```
UI Framework: WPF
Architecture: MVVM (Model-View-ViewModel)
Language: C# 10/11
XAML Version: 2006/2009 namespace
Target OS: Windows 10/11 (x64)
```

**Design System Reference**:
- Microsoft Fluent Design System
- Windows 11 design guidelines
- Material Design (for icons only)

**UI Controls to Use**:
- `Window` - Main window
- `Grid` - Layout
- `ToolBar` or `StackPanel` - Toolbar
- `DataGrid` - Job list
- `ProgressBar` - Progress indicators
- `TextBox` (read-only) - Log viewer
- `Button` - All buttons
- `ContextMenu` - Action menus
- `GroupBox` or `Border` - Section containers

**Third-party Libraries (Optional)**:
- MahApps.Metro (for modern styling)
- Material Design In XAML (for icons)
- HandyControl (for enhanced controls)

---

## 🎨 Design Specifications Summary

### Color Palette (Light Mode):
```
PRIMARY COLORS:
- Primary Blue: #2196F3 (buttons, links, active state)
- Success Green: #4CAF50 (completed status)
- Warning Orange: #FF9800 (processing, pause)
- Error Red: #F44336 (failed status)
- Info Purple: #9C27B0 (transcribing)
- Neutral Gray: #757575 (queued, disabled)

BACKGROUNDS:
- Window Background: #FAFAFA (light gray)
- Section Background: #FFFFFF (white)
- Alternate Row: #F9F9F9 (very light gray)
- Hover: #F5F5F5 (lighter gray)

BORDERS:
- Default: #E0E0E0 (light gray)
- Focus: #2196F3 (blue)
- Error: #F44336 (red)

TEXT:
- Primary: #212121 (dark gray, almost black)
- Secondary: #757575 (gray)
- Disabled: #BDBDBD (light gray)
- On Primary (white bg): #FFFFFF
```

### Typography:
```
FONT FAMILY:
- Primary: Segoe UI (Windows default)
- Monospace: Consolas (for log viewer)

FONT SIZES:
- Large Header: 16pt (Section titles)
- Header: 14pt (Group headers)
- Body: 12pt (Normal text, buttons)
- Small: 10pt (Helper text, timestamps)
- Tiny: 9pt (Sub-labels)

FONT WEIGHTS:
- Regular: 400 (default)
- Medium: 500 (sub-headers)
- SemiBold: 600 (important text)
- Bold: 700 (headers, emphasis)
```

### Spacing:
```
MARGINS:
- Section: 10px (between major sections)
- Control: 5px (between controls)

PADDING:
- Button: 12px horizontal, 6px vertical
- Section Container: 15px all sides
- Grid Cell: 8px horizontal, 4px vertical

BORDER RADIUS:
- Button: 4px
- Progress Bar: 8px
- Container: 4px
- Input: 3px
```

### Shadows (Subtle):
```
- Button Hover: 0 2px 4px rgba(0,0,0,0.1)
- Toolbar: 0 2px 4px rgba(0,0,0,0.08)
- Menu: 0 4px 8px rgba(0,0,0,0.15)
```

---

## 📏 Component Dimensions

### Toolbar:
```
Height: 50px
Button Width: 110px (with text) or 36px (icon only)
Button Height: 36px
Icon Size: 20x20px
Spacing: 8px between buttons
Separator Width: 1px, Height: 30px
```

### DataGrid:
```
Row Height: 65px (for 2-line content)
Header Height: 35px
Column Widths:
  - Checkbox: 40px (fixed)
  - Filename: 300px (min 200px, can grow)
  - Status: 140px (fixed)
  - Progress: 220px (min 180px)
  - Time: 90px (fixed)
  - Actions: 50px (fixed)
```

### Progress Bar:
```
Height: 24px (main progress)
Height: 16px (in DataGrid)
Corner Radius: 8px
Animation: Smooth fill (300ms ease)
```

### Log Viewer:
```
Height: 200px (default)
Min Height: 100px
Max Height: 400px
Font: Consolas, 10pt
Line Height: 18px
Padding: 10px
```

---

## 🎭 Interactive States

### Buttons:
```
NORMAL:
- Background: Primary color
- Text: White
- Shadow: None

HOVER:
- Background: Lighter (10%)
- Shadow: 0 2px 4px rgba(0,0,0,0.1)
- Cursor: Pointer
- Transition: 200ms

PRESSED:
- Background: Darker (10%)
- Shadow: Inset 0 2px 4px rgba(0,0,0,0.1)
- Scale: 0.98
- Transition: 100ms

DISABLED:
- Background: #E0E0E0
- Text: #BDBDBD
- Cursor: Not-allowed
- Opacity: 0.6
```

### DataGrid Rows:
```
NORMAL:
- Background: White
- Border: None

HOVER:
- Background: #F5F5F5
- Transition: 150ms

SELECTED:
- Background: #E3F2FD (light blue)
- Border: 1px solid #2196F3

ACTIVE (Processing):
- Border-left: 3px solid Status Color
- Background: Very light tint of status color
```

### Progress Bar:
```
IDLE:
- Fill: None (#E0E0E0 background)

ACTIVE:
- Fill: Animated gradient (status color)
- Shimmer effect (optional)

INDETERMINATE:
- Animated stripe pattern

COMPLETE:
- Fill: 100% with gentle pulse
- Color: Green
```

---

## 🎬 Animations

### Entrance Animations:
```
- Window: Fade in (300ms)
- Menu: Slide down + fade (200ms)
- Toast/Notification: Slide in from right (250ms)
```

### Progress Animations:
```
- Progress Bar Fill: Smooth increment (300ms ease-out)
- Status Dot Pulse: Scale 1.0 → 1.2 → 1.0 (1.5s loop)
- Shimmer: Left to right sweep (2s loop)
```

### Interaction Animations:
```
- Button Click: Scale 1.0 → 0.98 → 1.0 (200ms)
- Row Select: Background fade (150ms)
- Icon Hover: Slight bounce (200ms)
```

### Loading States:
```
- Spinner: Rotate 360° (1s linear infinite)
- Skeleton: Shimmer effect
- Indeterminate Progress: Stripe animation
```

---

## 🔤 Text & Labels

### Button Labels:
```
- "Add Files" (with ➕ icon)
- "Remove Selected" (with 🗑️ icon)
- "Start Processing" (with ▶️ icon)
- "Pause" (with ⏸️ icon)
- "Stop" (with ⏹️ icon)
- "Output Folder..." (with 📁 icon)
- "Settings" (with ⚙️ icon)
```

### Status Labels:
```
- "Queued" (Gray)
- "Converting" (Blue)
- "Transcribing" (Purple)
- "Completed" (Green)
- "Failed" (Red)
- "Paused" (Orange)
- "Canceled" (Gray)
```

### Section Headers:
```
- "📁 Files & Processing Queue"
- "📊 Overall Progress"
- "📝 Processing Log"
```

### Empty States:
```
Jobs Grid:
  "No files added yet
   Click 'Add Files' or drag & drop video files here"

Log Viewer:
  "No activity yet
   Logs will appear here when processing starts"
```

### Tooltips:
```
- Buttons: Show keyboard shortcut (e.g., "Add Files (Ctrl+O)")
- Status: Show detailed status message
- Progress: Show percentage and phase
- Time: Show start time and duration
- Disabled controls: Show reason why disabled
```

---

## ✅ Accessibility Requirements

### Keyboard Navigation:
```
- Tab order: Logical top-to-bottom, left-to-right
- Enter: Activate focused button
- Space: Toggle checkbox, click button
- Escape: Close menu/dialog
- Ctrl+O: Add Files
- Ctrl+S: Start Processing
- Delete: Remove Selected
```

### Screen Reader Support:
```
- All buttons have accessible names
- Status updates announced
- Progress changes announced (throttled)
- Error messages announced immediately
```

### High Contrast Mode:
```
- Respect system high contrast settings
- Sufficient color contrast (WCAG AA)
- Don't rely solely on color for information
```

---

## 📱 Reference Screenshots/Mockups

Tham khảo 2 documents đính kèm:
1. `mainwindow-detailed-spec.md` - Technical specifications
2. `mainwindow-visual-prototype.md` - ASCII art mockups

---

## 🎯 Priority Features

### Must Have (Priority 1):
- ✅ Basic layout with 4 sections
- ✅ Toolbar with main actions
- ✅ DataGrid with progress indicators
- ✅ Status colors and icons
- ✅ Overall progress summary

### Should Have (Priority 2):
- ✅ Action menus
- ✅ Log viewer with color coding
- ✅ Hover states and tooltips
- ✅ Responsive behavior
- ✅ Empty states

### Nice to Have (Priority 3):
- ✅ Animations
- ✅ Drag & drop visual feedback
- ✅ Keyboard shortcuts
- ✅ Dark mode variant
- ✅ Custom scrollbars

---

## 📦 Deliverables Expected

Từ AI Design Tool, tôi mong muốn nhận được:

### 1. Design Files:
- [ ] High-fidelity mockup (1200x800px)
- [ ] Component library (buttons, progress bars, etc.)
- [ ] All states visualized (idle, processing, completed)
- [ ] Color palette swatch
- [ ] Typography scale

### 2. Assets:
- [ ] Icons (SVG format, 16x16, 20x20, 24x24)
- [ ] Button states (normal, hover, pressed, disabled)
- [ ] Status indicators
- [ ] Export at 1x and 2x (for high DPI)

### 3. Specifications:
- [ ] Measurement annotations
- [ ] Color codes (HEX)
- [ ] Font specifications
- [ ] Spacing guidelines

### 4. Interactive Prototype (Optional):
- [ ] Clickable prototype showing interactions
- [ ] State transitions
- [ ] Menu behaviors

---

## ✨ Final Notes

- **Target Users**: IT professionals, video editors, educators
- **Usage Context**: Desktop workstation, multiple monitors
- **Session Duration**: 30-60 minutes (batch processing)
- **Critical Info**: Progress visibility, error messages, ETA
- **Tone**: Professional, clear, efficient, trustworthy

**Design Philosophy**: 
> "Clarity over creativity. Users should immediately understand status and actions without training."

---

**Document Version**: 1.0  
**Created**: 2025-11-17  
**For**: AI Design Tool (Figma AI, v0.dev, Galileo AI, etc.)  
**Status**: ✅ Ready for design generation
