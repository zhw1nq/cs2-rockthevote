# Memory Leak Optimization

## Vấn đề gặp phải
Plugin bị memory leaks gây ra lỗi "Dumping Leaks" trong CounterStrikeSharp:
- Timer không được kill properly
- Reference đến CCSPlayerController không được clear
- Event handlers không cleanup

## Các fix đã thực hiện

### 1. **Timer Management**
✅ Đảm bảo tất cả Timer có `TimerFlags.STOP_ON_MAPCHANGE`
✅ Kill timer trước khi tạo mới (tránh duplicate)
✅ Cleanup timer trong OnMapStart

**Files fixed:**
- `Features/RockTheVoteCommand.cs`
- `Core/EndMapVoteManager.cs`
- `Core/ChangeMapManager.cs`
- `Core/RandomStartMapManager.cs`

### 2. **Player Reference Cleanup**
✅ Clear `_initiatingPlayer` trong OnMapStart
✅ Null checks trước khi access player
✅ Clear VotedPlayers set khi map change

### 3. **State Management**
✅ Reset states trong OnMapStart:
  - `_pluginState.MapChangeScheduled = false`
  - `VotedPlayers.Clear()`
  - `Votes.Clear()`
  - `mapsElected.Clear()`

### 4. **Safe Player Operations**
✅ Try-catch cho menu operations
✅ Validate player.IsValid trước khi action
✅ Check player?.UserId != null

## Code Patterns để tránh leaks

### ❌ BAD - Memory Leak:
```csharp
// Timer không có STOP_ON_MAPCHANGE
_timer = plugin.AddTimer(5.0f, () => { DoSomething(); });

// Không kill timer cũ
_timer = plugin.AddTimer(5.0f, () => { DoSomething(); });
```

### ✅ GOOD - No Leak:
```csharp
// Có STOP_ON_MAPCHANGE
_timer = plugin.AddTimer(5.0f, () => { 
    DoSomething(); 
}, TimerFlags.STOP_ON_MAPCHANGE);

// Kill timer cũ trước khi tạo mới
_timer?.Kill();
_timer = null;
_timer = plugin.AddTimer(5.0f, () => { 
    DoSomething(); 
}, TimerFlags.STOP_ON_MAPCHANGE);
```

### ❌ BAD - Player Reference Leak:
```csharp
// Giữ reference đến player
private CCSPlayerController? _savedPlayer;

public void OnMapStart(string map)
{
    // Không clear reference
}
```

### ✅ GOOD - Clear References:
```csharp
private CCSPlayerController? _savedPlayer;

public void OnMapStart(string map)
{
    _savedPlayer = null; // Clear reference
    VotedPlayers.Clear(); // Clear collections
}
```

## Testing
```bash
# Build thành công
dotnet build
# 0 Warnings, 0 Errors

# Khi chạy trên server, không còn thấy:
# - Dumping Leaks
# - Vector leaks
# - Angles leaks  
# - GameEvents leaks
# - Virtual Functions leaks
```

## Best Practices

1. **Luôn dùng TimerFlags.STOP_ON_MAPCHANGE** cho mọi Timer
2. **Kill timer trước khi assign mới** để tránh duplicate
3. **Clear tất cả collections** trong OnMapStart
4. **Null player references** sau khi dùng xong
5. **Try-catch cho player operations** vì player có thể disconnect bất kỳ lúc nào
6. **Validate IsValid** trước mỗi player operation

## Version Info
- Version: 2.2.0
- Build: Success
- Memory Leaks: Fixed ✅

