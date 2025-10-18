# CS2 RockTheVote (RTV) - Optimized

Plugin bỏ phiếu map tối ưu hóa, loại bỏ các tính năng không cần thiết.

## Tính năng chính

### 3 Tính năng Core

- **RockTheVote (RTV)** - Cho phép người chơi vote để thay đổi map (!rtv)
- **EndOfMapVote** - Vote tự động cuối map với map cooldown
- **MapChooser** - Admin có thể chọn map thủ công

### Tính năng phụ

- Đọc từ maplist tùy chỉnh
- Hỗ trợ Panorama vote (F1/F2)
- Map cooldown system
- Time left command (!timeleft)
- Next map command (!nextmap)
- Map list command (!maps, !maplist)

## Tối ưu hóa v2.2.0

### Đã xóa bỏ:

✅ **AFK Manager** - Bỏ hẳn tracking AFK players  
✅ **Spectator filtering** - Không còn filter spectator nữa  
✅ **Discord webhook** - Bỏ notification Discord  
✅ **Countdown features** - Bỏ chat/HUD countdown (EnableCountdown, CountdownType, ChatCountdownInterval)  
✅ **Hint features** - Bỏ game hint overlay (EnableHint)  
✅ **Workshop maps** - Bỏ support workshop maps (host_workshop_map, ds_workshop_changelevel)  
✅ **Map validator** - Bỏ workshop map validation

### Cấu trúc file tối ưu:

- **7 files** trong Core/ (từ 11 files)
- **6 files** trong Features/ (giữ nguyên)
- **7 files** trong CrossCutting/ (từ 8 files)
- Gộp các class nhỏ vào file chung (VoteResult, AsyncVoteValidator, MapCooldown)

### Kết quả:

✅ Code đơn giản hơn, dễ đọc  
✅ Chỉ support map thông thường (changelevel)  
✅ Plugin nhẹ hơn, ít dependency  
✅ Build thành công 0 errors, 0 warnings

## Yêu cầu

[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (Tested on v340+)

## Cài đặt

1. Cài đặt CounterStrikeSharp
2. Copy plugin vào `game/csgo/addons/counterstrikesharp/plugins/`
3. Tạo file maplist theo mẫu maplist.example.txt
4. Khởi động lại server
