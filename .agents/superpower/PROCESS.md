# Superpower Process

## Mục đích

Quy trình này giúp mỗi thay đổi trong PvZ-Unity có chủ đích rõ ràng, ít rủi ro,
và có bằng chứng đã được kiểm tra. Áp dụng cho phân tích, sửa lỗi, tính năng,
refactor, tài nguyên và cấu hình Unity.

## 1. Làm rõ mục tiêu

- Đọc yêu cầu và nêu lại kết quả cần đạt, phạm vi, cùng tiêu chí chấp nhận.
- Xác định những giả định. Nếu một lựa chọn ảnh hưởng đáng kể đến sản phẩm, hỏi
  người dùng thay vì tự quyết.
- Không thay đổi tệp khi yêu cầu chỉ là giải thích, rà soát hoặc chẩn đoán.

## 2. Khảo sát trước khi hành động

- Kiểm tra cấu trúc, các tệp liên quan, quy ước hiện hữu và trạng thái Git.
- Tìm mã hoặc tài nguyên tương tự để tái sử dụng phong cách có sẵn.
- Bảo toàn các thay đổi chưa commit của người dùng; không hoàn tác, ghi đè hay
  định dạng lại phần không liên quan.

## 3. Lập kế hoạch có thể kiểm chứng

- Chia công việc thành các bước nhỏ theo thứ tự phụ thuộc.
- Với mỗi thay đổi, xác định tệp bị ảnh hưởng, hành vi kỳ vọng và cách xác minh.
- Ưu tiên giải pháp đơn giản nhất, tương thích với phiên bản Unity và kiến trúc hiện tại.

## 4. Thực hiện an toàn

- Chỉ sửa đúng phạm vi đã thống nhất; giữ thay đổi nhỏ, dễ review và dễ đảo ngược.
- Tôn trọng cấu trúc Unity: không sửa thủ công các tệp sinh tự động, `Library/`,
  `Temp/`, `obj/`, hoặc `.csproj` trừ khi yêu cầu nói rõ.
- Với gameplay, kiểm tra các trạng thái biên: khởi tạo, tạm dừng, kết thúc màn,
  đối tượng bị hủy, dữ liệu thiếu và gọi lặp.
- Không đưa khóa, mật khẩu, token hay dữ liệu nhạy cảm vào mã hoặc log.

## 5. Xác minh

- Chạy kiểm thử, build, lint hoặc kiểm tra phù hợp nhất với thay đổi khi môi trường cho phép.
- Với thay đổi Unity không thể tự động kiểm thử, ghi rõ kịch bản kiểm tra thủ công:
  thao tác, kết quả kỳ vọng và kết quả quan sát.
- Đọc lại diff để phát hiện thay đổi ngoài ý muốn, lỗi null, lỗi biên và tài nguyên bị thiếu.
- Không tuyên bố đã kiểm thử thành công nếu chưa thực sự chạy hoặc quan sát kết quả.

## 6. Bàn giao

- Báo cáo ngắn: kết quả đạt được, tệp chính đã đổi, cách xác minh và mọi phần
  chưa thể xác minh/rủi ro còn lại.
- Đính kèm đường dẫn tệp khi hữu ích để người dùng review nhanh.
- Nếu bị chặn, nêu nguyên nhân, bằng chứng đã kiểm tra và lựa chọn tiếp theo cần người dùng quyết định.

## Định nghĩa hoàn thành

Một yêu cầu chỉ hoàn thành khi hành vi yêu cầu đã được triển khai hoặc câu trả lời
đã được cung cấp; phạm vi không bị mở rộng; và có kết quả xác minh tương xứng được
báo cáo trung thực.

## Mandatory Plants vs. Zombies UI Rules

- Every screen, HUD, menu, popup, button, and UI component must feel playful,
  readable, animated, and whimsical; it must clearly belong to a garden-versus-
  zombie world.
- Favor lawn/plant greens, soil browns, sunny reward yellows, and warm or muted
  purple-gray zombie/danger accents. Avoid dashboard, cyberpunk, military,
  realistic-horror, neon, or cold-gradient visual language without an explicit
  in-game reason.
- Favor rounded shapes, wood/soil signage, natural textures, legible outlines,
  and hand-drawn character. Icons must match their meaning (sun/resources,
  plant/defense, zombie/enemy, shovel/remove).
- Gameplay UI must prioritize the playfield and planting grid. HUD and popups may
  not block lanes or make tile selection difficult. Popups need a clear close path
  and must never trap the player.
- Establish hierarchy: current goal and resources first; primary action stronger
  than secondary action; destructive or risky actions require confirmation.
- Use intentional Unity anchors, scaling, and safe areas. Do not hard-code a UI
  layout for only one resolution.
- Text must have sufficient contrast; never convey an important state by color alone.
  Interactive buttons require normal, disabled, and pressed feedback, plus hover or
  selected feedback where the platform supports it.
- Give brief, thematic visual feedback for success, insufficient resources, valid/
  invalid planting, pause, and game-over states. Use consistent terminology.
- Before handoff, verify and report: theme fit; unobstructed main flow; readable
  text/icons/states; relevant screen aspect ratios including long content; and clear
  licensing for every newly added asset or font.

## Mandatory Game Programming and Architecture Rules

### Design boundaries

- Build feature-oriented modules with explicit responsibilities. A class must have one primary reason to change; split it when it mixes domain rules, input, UI, persistence, audio, and object creation.
- Keep game rules independent from Unity presentation where practical. Domain and gameplay services decide what is allowed; MonoBehaviours adapt Unity events and lifecycle; views render state and forward user intent without owning game rules.
- UI must not mutate gameplay state directly. Route commands through a controller, coordinator, or use-case API that validates the action and publishes the result.
- Depend on interfaces or narrow abstractions at module boundaries. Do not create hidden global dependencies through static mutable state, arbitrary scene lookup, or singleton access inside domain logic.
- Use composition over inheritance. Inheritance is reserved for a stable, genuine is-a relationship with shared behavior; avoid deep base-class trees.

### Classes, objects, and data

- Give every class a descriptive noun/role name and a focused public API. Methods use verbs and make side effects visible through their name, return value, or event.
- Prefer small immutable value objects for coordinates, costs, timers, IDs, and configuration snapshots. Validate their invariants at construction or boundaries.
- Use ScriptableObject or serialized configuration for balanced values, prefabs, and static content. Do not hard-code tunable gameplay numbers throughout behavior code.
- Use factories, spawners, or pools as the single owner of runtime entity creation and destruction. A spawned entity must have explicit initialization, registration, cleanup, and ownership rules.
- Avoid God classes, boolean-flag state machines, and untyped string identifiers. Model finite gameplay states explicitly with enums, state objects, or transitions.
- Prefer events/messages for meaningful cross-module state changes. Subscribe and unsubscribe predictably; event listeners must not outlive the object they observe.

### Unity lifecycle and performance

- Respect Awake, OnEnable, Start, Update, OnDisable, and OnDestroy responsibilities. Initialize dependencies before use, cancel coroutines/timers, and unsubscribe on disable or destruction.
- Do not use Find, GetComponent, Instantiate, Destroy, allocations, or broad scene searches repeatedly in Update. Cache stable references and pool frequently used gameplay objects.
- Never rely on execution order accidentally. Declare dependencies explicitly and design a bootstrap/composition root for system initialization.
- Guard every external, serialized, asynchronous, or scene reference. Fail clearly during development when a required dependency is missing; avoid silent null paths.

### Logic correctness and change discipline

- Define ownership and valid state transitions before implementation: who creates, updates, damages, pauses, removes, and saves each entity; no two systems may own the same mutation without an explicit coordination rule.
- Keep authoritative game state separate from visual state. Rebuild or synchronize the view from the authoritative state instead of treating animation/UI as truth.
- Make time, randomness, and external services injectable or isolated whenever they affect game rules, so behavior can be tested and reproduced.
- Handle invalid commands, duplicate requests, missing dependencies, scene reloads, pause/resume, and object destruction safely and deterministically.
- Make the smallest compatible change. Do not rename, reformat, or refactor unrelated code while implementing a feature or fix.

### Required code review checklist

Before handoff, verify and report: clear class responsibility; correct boundary between game logic, Unity adapters, and UI; explicit ownership/lifecycle; validated state transitions and edge cases; cleanup of events/coroutines; safe null handling; no per-frame avoidable allocations/lookups; configuration separated from behavior; and tests or a reproducible manual scenario for the changed rule.
