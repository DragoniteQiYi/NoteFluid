using NAudio.Midi;
using NoteFluid.Core.Models;

namespace NoteFluid.Core.Utilities
{
    public class MidiInstrumentReader
    {
        public static List<InstrumentInfo> GetTrackInstruments(string filePath)
        {
            var instruments = new List<InstrumentInfo>();
            var midiFile = new MidiFile(filePath, false);

            for (int track = 0; track < midiFile.Tracks; track++)
            {
                // 第一步：先统计每个通道的音符数量
                var channelNoteCount = new Dictionary<int, int>();
                foreach (var midiEvent in midiFile.Events[track])
                {
                    if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
                    {
                        var noteOnEvent = (NoteOnEvent)midiEvent;
                        if (noteOnEvent.Velocity > 0)
                        {
                            int channel = noteOnEvent.Channel;
                            if (!channelNoteCount.ContainsKey(channel))
                                channelNoteCount[channel] = 0;
                            channelNoteCount[channel]++;
                        }
                    }
                }

                // 第二步：处理 PatchChange 事件
                var processedChannels = new HashSet<int>();

                // 先检查是否有PatchChange事件
                foreach (var midiEvent in midiFile.Events[track])
                {
                    if (midiEvent.CommandCode == MidiCommandCode.PatchChange)
                    {
                        var patchChange = (PatchChangeEvent)midiEvent;
                        int channel = patchChange.Channel;

                        if (processedChannels.Contains(channel))
                            continue;
                        processedChannels.Add(channel);

                        int patchNumber = patchChange.Patch;
                        string instrumentName;
                        bool isPercussion = (channel == 10);

                        if (isPercussion)
                        {
                            // 打击乐器通道特殊处理
                            patchNumber = -1; // 使用-1标记打击乐器
                            instrumentName = "Standard Drum Kit";
                        }
                        else
                        {
                            instrumentName = PatchChangeEvent.GetPatchName(patchNumber);
                        }

                        var instrumentInfo = new InstrumentInfo
                        {
                            InstrumentId = Random.Shared.Next(0, 25565),
                            PatchNumber = patchNumber,
                            InstrumentName = instrumentName,
                            Channel = channel,
                            NoteCount = channelNoteCount.TryGetValue(channel, out int value) ? value : 0,
                            IsPercussion = isPercussion
                        };

                        instruments.Add(instrumentInfo);
                    }
                }

                // 第三步：处理没有PatchChange但有音符的通道（特别是打击乐器通道）
                foreach (var kvp in channelNoteCount)
                {
                    int channel = kvp.Key;
                    if (!processedChannels.Contains(channel) && kvp.Value > 0)
                    {
                        bool isPercussion = (channel == 9);

                        var instrumentInfo = new InstrumentInfo
                        {
                            InstrumentId = Random.Shared.Next(0, 25565),
                            PatchNumber = isPercussion ? -1 : 0,
                            InstrumentName = isPercussion ? "Standard Drum Kit" : "Unknown Instrument",
                            Channel = channel,
                            NoteCount = kvp.Value,
                            IsPercussion = isPercussion
                        };

                        instruments.Add(instrumentInfo);
                    }
                }
            }

            return instruments;
        }
    }
}
