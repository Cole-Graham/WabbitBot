# BaseDiscordSelectComponent, DiscordActionRowComponent, DiscordButtonComponent, DiscordContainerComponent,
# DiscordFileComponent, DiscordLabelComponent, DiscordMediaGalleryComponent, DiscordSectionComponent,
# DiscordSeparatorComponent, DiscordTextDisplayComponent, DiscordTextInputComponent, DiscordThumbnailComponent

# BaseDiscordSelectComponent 

Class BaseDiscordSelectComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a base class for all select-menus.

public abstract class BaseDiscordSelectComponent : DiscordComponent
Inheritance
object DiscordComponent BaseDiscordSelectComponent
Derived
DiscordChannelSelectComponent DiscordMentionableSelectComponent DiscordRoleSelectComponent DiscordSelectComponent DiscordUserSelectComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
BaseDiscordSelectComponent()
Properties
Disabled
Whether this dropdown can be interacted with.

MaximumSelectedValues
The maximum amount of options that can be selected. Must be greater than or equal to zero or MinimumSelectedValues. Defaults to one.

MinimumSelectedValues
The minimum amount of options that can be selected. Must be less than or equal to MaximumSelectedValues. Defaults to one.

Placeholder
The text to show when no option is selected.

Required
Whether this component is required. Only affects usage in modals. Defaults to true.

----------------------------------------------------------------------------------------

# DiscordActionRowComponent

Class DiscordActionRowComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a row of components. Action rows can have up to five components.

public sealed class DiscordActionRowComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordActionRowComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
DiscordActionRowComponent(IEnumerable<DiscordComponent>)
Properties
Components
The components contained within the action row.

----------------------------------------------------------------------------------------

# DiscordButtonComponent

Class DiscordButtonComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a button that can be pressed. Fires ComponentInteractionCreatedEventArgs when pressed.

public class DiscordButtonComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordButtonComponent
Derived
DiscordLinkButtonComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
DiscordButtonComponent(DiscordButtonComponent)
Constucts a new button based on another button.

DiscordButtonComponent(DiscordButtonStyle, string, string, bool, DiscordComponentEmoji)
Constructs a new button with the specified options.

Properties
Disabled
Whether this button can be pressed.

Emoji
The emoji to add to the button. Can be used in conjunction with a label, or as standalone. Must be added if label is not specified.

Label
The text to apply to the button. If this is not specified Emoji becomes required.

Style
The style of the button.

Methods
Disable()
Disables this component.

Enable()
Enables this component if it was disabled before.

----------------------------------------------------------------------------------------

# DiscordContainerComponent

Class DiscordContainerComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
A container for other components.

public class DiscordContainerComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordContainerComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
DiscordContainerComponent(IReadOnlyList<DiscordComponent>, bool, DiscordColor?)
Properties
Color
The accent color for this container, similar to an embed.

Components
Gets the components of this container.

IsSpoilered
Gets whether this container is spoilered.

----------------------------------------------------------------------------------------

# DiscordFileComponent

Class DiscordFileComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a component that will display a single file.

public sealed class DiscordFileComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordFileComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
DiscordFileComponent(string, bool)
Properties
File
Gets the file associated with this component. It may be an arbitrary URL or an attachment:// reference.

IsSpoilered
Gets whether this file is spoilered.

----------------------------------------------------------------------------------------

# DiscordLabelComponent

Class DiscordLabelComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a label in a modal.

public class DiscordLabelComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordLabelComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
DiscordLabelComponent(BaseDiscordSelectComponent, string, string?)
DiscordLabelComponent(DiscordTextInputComponent, string, string?)
Properties
Component
Gets the component contained within the label. At this time, this may only be BaseDiscordSelectComponent or DiscordTextInputComponent.

ComponentType
Description
Gets or sets the description of the label.

Label
Gets or sets the label.

----------------------------------------------------------------------------------------

# DiscordMediaGalleryComponent

Class DiscordMediaGalleryComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a gallery of various media.

public sealed class DiscordMediaGalleryComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordMediaGalleryComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id
Constructors
DiscordMediaGalleryComponent(params IEnumerable<DiscordMediaGalleryItem>)
Constructs a new media gallery component.

DiscordMediaGalleryComponent(IEnumerable<DiscordMediaGalleryItem>, int)
Constructs a new media gallery component.

Properties
Items
Gets the items in the gallery.

----------------------------------------------------------------------------------------

# DiscordSectionComponent

Class DiscordSectionComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
A section for components (as of now, just text) and an accessory on the side.

public class DiscordSectionComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordSectionComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id

# DiscordSectionComponent constructor:

DiscordSectionComponent
Constructors
Constructor DiscordSectionComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
DiscordSectionComponent(DiscordComponent, DiscordComponent) 
Constructs a new section component.

public DiscordSectionComponent(DiscordComponent textDisplayComponent, DiscordComponent accessory)
Parameters
textDisplayComponent DiscordComponent
accessory DiscordComponent
The accessory to this section. At this time, this must either be a DiscordThumbnailComponent or a DiscordButtonComponent.

DiscordSectionComponent(string, DiscordComponent) 
Constructs a new DiscordSectionComponent

public DiscordSectionComponent(string text, DiscordComponent accessory)
Parameters
text string
The text for this section.

accessory DiscordComponent
The accessory for this section.

DiscordSectionComponent(IReadOnlyList<DiscordComponent>, DiscordComponent) 
Constructs a new section component.

public DiscordSectionComponent(IReadOnlyList<DiscordComponent> sections, DiscordComponent accessory)
Parameters
sections IReadOnlyList<DiscordComponent>
The sections (generally text) that this section contains.

accessory DiscordComponent
The accessory to this section. At this time, this must either be a DiscordThumbnailComponent or a DiscordButtonComponent.

# DiscordSectionComponent properties:
# Property

DiscordSectionComponent
Properties
Property Accessory
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Accessory 
Gets the accessory for this section.

[JsonProperty("accessory", NullValueHandling = NullValueHandling.Ignore)]
public DiscordComponent? Accessory { get; }
Property Value
DiscordComponent
Remarks
Accessories take the place of a thumbnail (that is, are positioned as a thumbnail would be) regardless of component. At this time, only DiscordButtonComponent and DiscordThumbnailComponent are supported.

# Components

DiscordSectionComponent
Properties
Property Components
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Components 
Gets the components for this section. As of now, this is only text components, but may allow for more components in the future.

[JsonProperty("components", NullValueHandling = NullValueHandling.Ignore)]
public IReadOnlyList<DiscordComponent> Components { get; }
Property Value
IReadOnlyList<DiscordComponent>
Remarks
This is a Discord limitation.

----------------------------------------------------------------------------------------

# DiscordSeparatorComponent:

Class DiscordSeparatorComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a division between components. Can optionally be rendered as a dividing line.

public class DiscordSeparatorComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordSeparatorComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id

# DiscordSeparatorComponent constructor:

DiscordSeparatorComponent
Constructors
Constructor DiscordSeparatorComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
DiscordSeparatorComponent(bool, DiscordSeparatorSpacing) 
public DiscordSeparatorComponent(bool divider = false, DiscordSeparatorSpacing spacing = DiscordSeparatorSpacing.Small)
Parameters
divider bool
spacing DiscordSeparatorSpacing

# DiscordSeparatorComponent properties:
# Discord Property Divider


DiscordSeparatorComponent
Properties
Property Divider
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Divider 
Whether the separator renders as a dividing line.

[JsonProperty("divider", NullValueHandling = NullValueHandling.Ignore)]
public bool Divider { get; }
Property Value
bool

# DiscordSeparatorSpacing


Enum DiscordSeparatorSpacing
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents the spacing for a separator.

public enum DiscordSeparatorSpacing
Extension Methods
ExtensionMethods.GetName<T>(T)
Fields
Large = 2
A large spacing, equivalent to 33px or ~2 lines of text.

Small = 1
A small spacing, equivalent to 17px, or ~1 line of text.

----------------------------------------------------------------------------------------

# DiscordTextDisplayComponent:

Class DiscordTextDisplayComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a block of text.

public sealed class DiscordTextDisplayComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordTextDisplayComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id

# DiscordTextDisplayComponent constructor:

DiscordTextDisplayComponent
Constructors
Constructor DiscordTextDisplayComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
DiscordTextDisplayComponent(string) 
public DiscordTextDisplayComponent(string content)
Parameters
content string

# DiscordTextDisplayComponent properties:
# Property Content

DiscordTextDisplayComponent
Properties
Property Content
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Content 
Gets the content for this text display. This can be up to 4000 characters, summed by all text displays in a message.
One text display could contain 4000 characters, or 10 displays of 400 characters each for example.

[JsonProperty("content")]
public string Content { get; }
Property Value
string

----------------------------------------------------------------------------------------

# DiscordTextInputComponent:

Class DiscordTextInputComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
A text-input field. Like selects, this can only be used once per action row.

public sealed class DiscordTextInputComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordTextInputComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id

# DiscordTextInputComponent() constructor:

DiscordTextInputComponent
Constructors
Constructor DiscordTextInputComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
DiscordTextInputComponent() 
public DiscordTextInputComponent()
DiscordTextInputComponent(string, string?, string?, bool, DiscordTextInputStyle, int, int?) 
Constructs a new text input field.

public DiscordTextInputComponent(string customId, string? placeholder = null, string? value = null, bool required = true, DiscordTextInputStyle style = DiscordTextInputStyle.Short, int min_length = 0, int? max_length = null)
Parameters
customId string
The ID of this field.

placeholder string
Placeholder text for the field.

value string
A pre-filled value for this field.

required bool
Whether this field is required.

style DiscordTextInputStyle
The style of this field. A single-ling short, or multi-line paragraph.

min_length int
The minimum input length.

max_length int?
The maximum input length. Must be greater than the minimum, if set.

# DiscordSeparatorComponent properties:
# MaximumLength

MaximumLength 
Optional maximum length for this input. Must be a positive integer, if set.

[JsonProperty("max_length", NullValueHandling = NullValueHandling.Ignore)]
public int? MaximumLength { get; set; }
Property Value
int?

# MinimumLength

DiscordTextInputComponent
Properties
Property MinimumLength
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
MinimumLength 
Optional minimum length for this input.

[JsonProperty("min_length", NullValueHandling = NullValueHandling.Ignore)]
public int MinimumLength { get; set; }
Property Value
int

# Placeholder

DiscordTextInputComponent
Properties
Property Placeholder
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Placeholder 
Optional placeholder text for this input.

[JsonProperty("placeholder", NullValueHandling = NullValueHandling.Ignore)]
public string? Placeholder { get; set; }
Property Value
string

# Required

DiscordTextInputComponent
Properties
Property Required
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Required 
Whether this input is required.

[JsonProperty("required")]
public bool Required { get; set; }
Property Value
bool

# Style

DiscordTextInputComponent
Properties
Property Style
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Style 
Style of this input.

[JsonProperty("style")]
public DiscordTextInputStyle Style { get; set; }
Property Value
DiscordTextInputStyle

# Value

DiscordTextInputComponent
Properties
Property Value
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Value 
Pre-filled value for this input.

[JsonProperty("value")]
public string? Value { get; set; }
Property Value
string

----------------------------------------------------------------------------------------

# DiscordThumbnailComponent:

Class DiscordThumbnailComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents a thumbnail.

public class DiscordThumbnailComponent : DiscordComponent
Inheritance
object DiscordComponent DiscordThumbnailComponent
Inherited Members
DiscordComponent.Type  DiscordComponent.CustomId  DiscordComponent.Id

# DiscordThumbnailComponent constructor:

DiscordThumbnailComponent
Constructors
Constructor DiscordThumbnailComponent
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
DiscordThumbnailComponent(string, string?, bool) 
public DiscordThumbnailComponent(string url, string? description = null, bool spoiler = false)
Parameters
url string
description string
spoiler bool
DiscordThumbnailComponent(DiscordUnfurledMediaItem, string?, bool) 
public DiscordThumbnailComponent(DiscordUnfurledMediaItem media, string? description = null, bool spoiler = false)
Parameters
media DiscordUnfurledMediaItem
description string
spoiler bool


Class DiscordUnfurledMediaItem
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Represents an unfurled url; can be arbitrary URL or attachment:// schema.

public sealed class DiscordUnfurledMediaItem
Inheritance
object DiscordUnfurledMediaItem
Constructors
DiscordUnfurledMediaItem(string)
Properties
Url
Gets the URL of the media item.


DiscordUnfurledMediaItem
Constructors
Constructor DiscordUnfurledMediaItem
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
DiscordUnfurledMediaItem(string) 
public DiscordUnfurledMediaItem(string url)
Parameters
url string

# DiscordThumbnailComponent properties:
# Description

DiscordThumbnailComponent
Properties
Property Description
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Description 
Gets the description (alt-text) for this thumbnail.

[JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
public string? Description { get; }
Property Value
string

# Media

DiscordThumbnailComponent
Properties
Property Media
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Media 
The image for this thumbnail.

[JsonProperty("media", NullValueHandling = NullValueHandling.Ignore)]
public DiscordUnfurledMediaItem Media { get; }
Property Value
DiscordUnfurledMediaItem

# Spoiler

DiscordThumbnailComponent
Properties
Property Spoiler
Namespace DSharpPlus.Entities
AssemblyDSharpPlus.dll
Spoiler 
Gets whether this thumbnail is spoilered.

[JsonProperty("spoiler", NullValueHandling = NullValueHandling.Ignore)]
public bool Spoiler { get; }
Property Value
bool