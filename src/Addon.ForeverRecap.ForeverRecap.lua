-- Forever Recap: turns the server's "[Recap #12] Name (level 23 Warrior) was slain by ..." system lines into a
-- clickable [Recap] link. Clicking asks whether to watch, then asks the server for ".recap 12"; the server replays
-- the death (DeathRecap.cpp). While watching, a "Stop watching" button sends ".recap stop".
local ADDON = "ForeverRecap"
local names = {}   -- recap id -> player name, for the popup text

-- Server commands go through TrinityCore's addon command channel ("i" + 4-character echo + command without the
-- dot): unlike /say it also works while dead.
local function Send(command)
	C_ChatInfo.SendAddonMessage("TrinityCore", "iFRcp" .. command, "WHISPER", UnitName("player"))
end

-- "Stop watching" button, top center
local stop = CreateFrame("Button", "ForeverRecapStopButton", UIParent, "UIPanelButtonTemplate")
stop:SetSize(160, 28)
stop:SetPoint("TOP", UIParent, "TOP", 0, -120)
stop:SetText("Stop watching")
stop:SetScript("OnClick", function() Send("recap stop") end)
stop:Hide()

StaticPopupDialogs["FOREVER_RECAP_WATCH"] = {
	text = "Watch how %s died?",
	button1 = "Watch",
	button2 = CANCEL or "Cancel",
	OnAccept = function(self, data) Send("recap " .. data) end,
	timeout = 0,
	whileDead = true,
	hideOnEscape = true,
	preferredIndex = 3,
}

local function ShowPopup(id)
	local dialog = StaticPopup_Show("FOREVER_RECAP_WATCH", names[id] or ("recap #" .. id))
	if dialog then dialog.data = id end
end

-- clicks on |Haddon:ForeverRecap:<id>|h
EventRegistry:RegisterCallback("SetItemRef", function(_, link)
	local id = link and link:match("^addon:" .. ADDON .. ":(%d+)")
	if id then ShowPopup(id) end
end, ADDON)

local function Filter(_, _, msg, ...)
	if type(msg) ~= "string" then return false end

	local id = msg:match("%[Recap #(%d+)%]")
	if id then
		-- "|cffc9a0ff[Recap #12]|r |cffc69b6dName Surname|r|cffff4040 (level 23 Warrior) was slain by ... Type .recap 12 to watch.|r"
		local name = msg:match("%[Recap #%d+%]|r |c%x%x%x%x%x%x%x%x(.-)|r") or msg:match("%[Recap #%d+%]%s*([^%(]+) %(level")
		if name then names[id] = strtrim(name) end
		local link = "|cffc9a0ff|Haddon:" .. ADDON .. ":" .. id .. "|h[Recap]|h|r"
		local replaced
		msg, replaced = msg:gsub("|c%x%x%x%x%x%x%x%x%[Recap #%d+%]|r", link, 1)
		if replaced == 0 then msg = msg:gsub("%[Recap #%d+%]", link, 1) end
		msg = msg:gsub(" Type %.recap %d+ to watch%.", "")
		return false, msg, ...
	end

	if msg:find("^Watching recap #%d+") then
		stop:Show()
		return false, (msg:gsub(" Type %.recap stop to stop%.", "")), ...
	elseif msg == "Recap finished." or msg:find("^Recap stopped") then
		stop:Hide()
	end
	return false
end

local addFilter = ChatFrame_AddMessageEventFilter or (ChatFrameUtil and ChatFrameUtil.AddMessageEventFilter)
if addFilter then
	addFilter("CHAT_MSG_SYSTEM", Filter)
end

-- a reload or relog while watching ends the replay on the server
local f = CreateFrame("Frame")
f:RegisterEvent("PLAYER_ENTERING_WORLD")
f:SetScript("OnEvent", function() stop:Hide() end)

-- This beta's Hardcore code shows a "HARDCORE_DEATH" popup on death that the build doesn't define (Lua error on
-- every death on a Hardcore realm). Provide a plain one until Blizzard ships theirs.
if StaticPopupDialogs and not StaticPopupDialogs["HARDCORE_DEATH"] then
	StaticPopupDialogs["HARDCORE_DEATH"] = {
		text = "You have died. On a Hardcore realm this character can't be brought back.",
		button1 = OKAY or "Okay",
		timeout = 0,
		whileDead = true,
		hideOnEscape = true,
		preferredIndex = 3,
	}
end
