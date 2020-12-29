﻿'
' Copyright (C) 2013-2021 getMaNGOS <http://www.getmangos.eu>
'
' This program is free software; you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation; either version 2 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with this program; if not, write to the Free Software
' Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
'
Imports mangosVB.Common.BaseWriter

Public Module WS_Weather

    Public Enum WeatherSounds As Integer
        WEATHER_SOUND_NOSOUND = 0
        WEATHER_SOUND_RAINLIGHT = 8533
        WEATHER_SOUND_RAINMEDIUM = 8534
        WEATHER_SOUND_RAINHEAVY = 8535
        WEATHER_SOUND_SNOWLIGHT = 8536
        WEATHER_SOUND_SNOWMEDIUM = 8537
        WEATHER_SOUND_SNOWHEAVY = 8538
        WEATHER_SOUND_SANDSTORMLIGHT = 8556
        WEATHER_SOUND_SANDSTORMMEDIUM = 8557
        WEATHER_SOUND_SANDSTORMHEAVY = 8558
    End Enum

    Public Enum WeatherState As Integer
        WEATHER_STATE_FINE = 0
        WEATHER_STATE_LIGHT_RAIN = 3
        WEATHER_STATE_MEDIUM_RAIN = 4
        WEATHER_STATE_HEAVY_RAIN = 5
        WEATHER_STATE_LIGHT_SNOW = 6
        WEATHER_STATE_MEDIUM_SNOW = 7
        WEATHER_STATE_HEAVY_SNOW = 8
        WEATHER_STATE_LIGHT_SANDSTORM = 22
        WEATHER_STATE_MEDIUM_SANDSTORM = 41
        WEATHER_STATE_HEAVY_SANDSTORM = 42
        WEATHER_STATE_THUNDERS = 86
        WEATHER_STATE_BLACKRAIN = 90
    End Enum

    Public Enum WeatherType As Integer
        WEATHER_TYPE_FINE = 0
        WEATHER_TYPE_RAIN = 1
        WEATHER_TYPE_SNOW = 2
        WEATHER_TYPE_STORM = 3
        WEATHER_TYPE_THUNDERS = 86
        WEATHER_TYPE_BLACKRAIN = 90
    End Enum

    Public WeatherZones As New Dictionary(Of Integer, WeatherZone)

    Public Class WeatherSeasonChances
        Public RainChance As Integer
        Public SnowChance As Integer
        Public StormChance As Integer

        Public Sub New(ByVal RainChance As Integer, ByVal SnowChance As Integer, ByVal StormChance As Integer)
            Me.RainChance = RainChance
            Me.SnowChance = SnowChance
            Me.StormChance = StormChance
        End Sub
    End Class

    Public Class WeatherZone
        Public ZoneID As Integer
        Public Seasons(3) As WeatherSeasonChances
        Public CurrentWeather As WeatherState = WeatherState.WEATHER_STATE_FINE
        Public CurrentWeatherType As WeatherType = WeatherType.WEATHER_TYPE_FINE
        Public Intensity As Single = 0.0F

        Public Sub New(ByVal ZoneID As Integer)
            Me.ZoneID = ZoneID
        End Sub

        Public Sub Update()
            If ChangeWeather() Then
                SendUpdate()
            End If
        End Sub

        Public Function ChangeWeather() As Boolean
            ' Weather statistics:
            '- 30% - no change
            '- 30% - weather gets better (if not fine) or change weather type
            '- 30% - weather worsens (if not fine)
            '- 10% - radical change (if not fine)
            Dim u As Integer = Rnd.Next(0, 100)

            If u < 30 Then Exit Function 'No change

            'remember old values
            Dim oldWeather As WeatherState = CurrentWeather
            Dim oldIntensity As Single = Intensity

            '78 days between January 1st and March 20nd; 365/4=91 days by season
            Dim TimeSince1Jan As Integer = CInt(Fix(Now.Subtract(New Date(Now.Year, 1, 1)).TotalDays))
            Dim Season As Integer = ((TimeSince1Jan - 78 + 365) \ 91) Mod 4

            If u < 60 AndAlso Intensity < 0.333333343F Then 'Get fine
                CurrentWeather = WeatherState.WEATHER_STATE_FINE
                Intensity = 0.0F
            End If

            If u < 60 AndAlso CurrentWeather <> WeatherState.WEATHER_STATE_FINE Then 'Get better
                Intensity -= 0.333333343F
                Return True
            End If

            If u < 90 AndAlso CurrentWeather <> WeatherState.WEATHER_STATE_FINE Then 'Get worse
                Intensity += 0.333333343F
                Return True
            End If

            If CurrentWeather <> WeatherState.WEATHER_STATE_FINE Then
                ' Radical change:
                '- if light -> heavy
                '- if medium -> change weather type
                '- if heavy -> 50% light, 50% change weather type

                If Intensity < 0.333333343F Then
                    Intensity = 0.9999F 'Go nuts
                    Return True
                Else
                    If Intensity > 0.6666667F Then
                        Dim v As Integer = Rnd.Next(0, 100)
                        If v < 50 Then 'Severe change, but how severe?
                            Intensity -= 0.6666667F
                            Return True
                        End If
                    End If
                    CurrentWeather = WeatherState.WEATHER_STATE_FINE 'Clear up
                    Intensity = 0.0F
                End If
            End If

            'At this point, only weather that isn't doing anything remains but that have weather data
            Dim chance1 As Integer = Seasons(Season).RainChance
            Dim chance2 As Integer = chance1 + Seasons(Season).SnowChance
            Dim chance3 As Integer = chance2 + Seasons(Season).StormChance

            Dim r As Integer = Rnd.Next(0, 100)
            If r < chance1 Then
                CurrentWeatherType = WeatherType.WEATHER_TYPE_RAIN
            ElseIf r < chance2 Then
                CurrentWeatherType = WeatherType.WEATHER_TYPE_SNOW
            ElseIf r < chance3 Then
                CurrentWeatherType = WeatherType.WEATHER_TYPE_STORM
            Else
                CurrentWeatherType = WeatherType.WEATHER_TYPE_FINE
            End If

            ' New weather statistics (if not fine):
            '- 85% light
            '- 7% medium
            '- 7% heavy
            ' If fine 100% sun (no fog)

            If CurrentWeatherType = WeatherType.WEATHER_TYPE_FINE Then
                Intensity = 0.0F
            ElseIf u < 90 Then
                Intensity = CSng(Rnd.NextDouble() * 0.3333F)
            Else
                'Severe change, but how severe?
                r = Rnd.Next(0, 100)
                If r < 50 Then
                    Intensity = CSng(Rnd.NextDouble() * 0.3333F) + 0.3334F
                Else
                    Intensity = CSng(Rnd.NextDouble() * 0.3333F) + 0.6667F
                End If
            End If

            CurrentWeather = GetWeatherState()

            'return true only in case weather changes
            Return ((CurrentWeather <> oldWeather) OrElse (Intensity <> oldIntensity))
        End Function

        Public Function GetWeatherState() As WeatherState
            If (Intensity < 0.27F) Then Return WeatherState.WEATHER_STATE_FINE

            Select Case (CurrentWeatherType)
                Case WeatherType.WEATHER_TYPE_RAIN ' Rain
                    If (Intensity < 0.4F) Then
                        Return WeatherState.WEATHER_STATE_LIGHT_RAIN
                    ElseIf (Intensity < 0.7F) Then
                        Return WeatherState.WEATHER_STATE_MEDIUM_RAIN
                    Else
                        Return WeatherState.WEATHER_STATE_HEAVY_RAIN
                    End If
                Case WeatherType.WEATHER_TYPE_SNOW ' Snow
                    If (Intensity < 0.4F) Then
                        Return WeatherState.WEATHER_STATE_LIGHT_SNOW
                    ElseIf (Intensity < 0.7F) Then
                        Return WeatherState.WEATHER_STATE_MEDIUM_SNOW
                    Else
                        Return WeatherState.WEATHER_STATE_HEAVY_SNOW
                    End If
                Case WeatherType.WEATHER_TYPE_STORM ' Storm
                    If (Intensity < 0.4F) Then
                        Return WeatherState.WEATHER_STATE_LIGHT_SANDSTORM
                    ElseIf (Intensity < 0.7F) Then
                        Return WeatherState.WEATHER_STATE_MEDIUM_SANDSTORM
                    Else
                        Return WeatherState.WEATHER_STATE_HEAVY_SANDSTORM
                    End If
                Case WeatherType.WEATHER_TYPE_BLACKRAIN
                    Return WeatherState.WEATHER_STATE_BLACKRAIN
                Case WeatherType.WEATHER_TYPE_THUNDERS
                    Return WeatherState.WEATHER_STATE_THUNDERS
                Case WeatherType.WEATHER_TYPE_FINE ' Fine
                Case Else
                    Return WeatherState.WEATHER_STATE_FINE
            End Select

        End Function

        Public Function GetSound() As Integer
            Select Case CurrentWeather
                Case WeatherType.WEATHER_TYPE_RAIN
                    If Intensity < 0.333333343F Then
                        Return WeatherSounds.WEATHER_SOUND_RAINLIGHT
                    ElseIf Intensity < 0.6666667F Then
                        Return WeatherSounds.WEATHER_SOUND_RAINMEDIUM
                    Else
                        Return WeatherSounds.WEATHER_SOUND_RAINHEAVY
                    End If
                Case WeatherType.WEATHER_TYPE_SNOW
                    If Intensity < 0.333333343F Then
                        Return WeatherSounds.WEATHER_SOUND_SNOWLIGHT
                    ElseIf Intensity < 0.6666667F Then
                        Return WeatherSounds.WEATHER_SOUND_SNOWMEDIUM
                    Else
                        Return WeatherSounds.WEATHER_SOUND_SNOWHEAVY
                    End If
                Case WeatherType.WEATHER_TYPE_STORM
                    If Intensity < 0.333333343F Then
                        Return WeatherSounds.WEATHER_SOUND_SANDSTORMLIGHT
                    ElseIf Intensity < 0.6666667F Then
                        Return WeatherSounds.WEATHER_SOUND_SANDSTORMMEDIUM
                    Else
                        Return WeatherSounds.WEATHER_SOUND_SANDSTORMHEAVY
                    End If
                Case Else
                    Return WeatherSounds.WEATHER_SOUND_NOSOUND
            End Select
        End Function

        Public Sub SendUpdate()
            Dim SMSG_WEATHER As New PacketClass(OPCODES.SMSG_WEATHER)
            SMSG_WEATHER.AddInt32(CurrentWeather)
            SMSG_WEATHER.AddSingle(Intensity)
            SMSG_WEATHER.AddInt32(GetSound())

            Try
                CHARACTERs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)

                Try
                    For Each Character As KeyValuePair(Of ULong, CharacterObject) In CHARACTERs
                        If Character.Value.Client IsNot Nothing AndAlso Character.Value.ZoneID = ZoneID Then
                            Character.Value.Client.SendMultiplyPackets(SMSG_WEATHER)
                        End If
                    Next
                Catch ex As Exception
                    Log.WriteLine(LogType.CRITICAL, "Error updating Weather.{0}{1}", vbNewLine, ex.ToString)
                Finally
                    CHARACTERs_Lock.ReleaseReaderLock()
                End Try

            Catch ex As ApplicationException
                Log.WriteLine(LogType.WARNING, "Update: Weather Manager timed out")
            Catch ex As Exception
                Log.WriteLine(LogType.CRITICAL, "Error updating Weather.{0}{1}", vbNewLine, ex.ToString)
            End Try

            SMSG_WEATHER.Dispose()
        End Sub
    End Class

    Public Sub SendWeather(ByVal ZoneID As Integer, ByRef Client As ClientClass)
        If Not WeatherZones.ContainsKey(ZoneID) Then Exit Sub
        Dim Weather As WeatherZone = WeatherZones(ZoneID)

        Dim SMSG_WEATHER As New PacketClass(OPCODES.SMSG_WEATHER)
        SMSG_WEATHER.AddInt32(Weather.CurrentWeather)
        SMSG_WEATHER.AddSingle(Weather.Intensity)
        SMSG_WEATHER.AddInt32(Weather.GetSound())

        Client.Send(SMSG_WEATHER)
        SMSG_WEATHER.Dispose()
    End Sub

End Module
