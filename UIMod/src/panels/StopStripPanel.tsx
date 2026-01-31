import React, { useState, useEffect, useRef } from 'react';
import { bindValue, useValue } from "cs2/api";
import 'style/StopStripPanel.scss';

interface StationData {
    name: string;
}

interface LineStationInfo {
    stations: StationData[];
    currentStopIndex: number;
    lineColor: string;
}

const LineStationInfo$ = bindValue<string>('fpc', 'LineStationInfo');

const StopStripPanel: React.FC = () => {
    const [blinkDotIndex, setBlinkDotIndex] = useState(-1);
    const [isBlinking, setIsBlinking] = useState(false);
    const [blinkCycleKey, setBlinkCycleKey] = useState(0);
    const blinkTimeoutRef = useRef<number | null>(null);
    const isComponentMounted = useRef(true);
    const canStartNewBlinkRef = useRef(true);
    const graceTimeoutRef = useRef<number | null>(null);
    const blinkDotIndexRef = useRef(-1);
    const lastStopNameRef = useRef<string>("");
    const wasAtStationRef = useRef(false);

    const lineStationInfoStr = useValue(LineStationInfo$);

    const lineStationInfo: LineStationInfo | null = React.useMemo(() => {
        if (!lineStationInfoStr || lineStationInfoStr === "") {
            return null;
        }
        try {
            return JSON.parse(lineStationInfoStr) as LineStationInfo;
        } catch {
            return null;
        }
    }, [lineStationInfoStr]);

    useEffect(() => {
        if (!lineStationInfo) return;

        const currentIndex = lineStationInfo.currentStopIndex ?? -1;

        if (currentIndex >= 0) {
            blinkDotIndexRef.current = currentIndex;
            setBlinkDotIndex(currentIndex);
            lastStopNameRef.current = lineStationInfo.stations[currentIndex]?.name ?? "";
            wasAtStationRef.current = true;

            if (graceTimeoutRef.current !== null) {
                window.clearTimeout(graceTimeoutRef.current);
                graceTimeoutRef.current = null;
            }

            if (!canStartNewBlinkRef.current) {
                canStartNewBlinkRef.current = true;
                setBlinkCycleKey(k => k + 1);
            }
        } else if (wasAtStationRef.current) {
            if (lastStopNameRef.current && lineStationInfo.stations.length > 0) {
                const newIdx = lineStationInfo.stations.findIndex(
                    s => s.name === lastStopNameRef.current
                );
                if (newIdx >= 0) {
                    if (newIdx !== blinkDotIndexRef.current) {
                        blinkDotIndexRef.current = newIdx;
                        setBlinkDotIndex(newIdx);
                    }
                } else {
                    canStartNewBlinkRef.current = false;
                    blinkDotIndexRef.current = -1;
                    setBlinkDotIndex(-1);
                    wasAtStationRef.current = false;
                }
            }

            if (graceTimeoutRef.current === null && canStartNewBlinkRef.current) {
                graceTimeoutRef.current = window.setTimeout(() => {
                    canStartNewBlinkRef.current = false;
                    graceTimeoutRef.current = null;
                }, 1250);
            }
        }
    }, [lineStationInfo]);

    useEffect(() => {
        isComponentMounted.current = true;

        const startRedPhase = () => {
            if (!isComponentMounted.current) return;

            if (!canStartNewBlinkRef.current) {
                setIsBlinking(false);
                blinkDotIndexRef.current = -1;
                setBlinkDotIndex(-1);
                wasAtStationRef.current = false;
                return;
            }

            setIsBlinking(true);

            blinkTimeoutRef.current = window.setTimeout(() => {
                if (!isComponentMounted.current) return;
                setIsBlinking(false);
                blinkTimeoutRef.current = window.setTimeout(startRedPhase, 1000);
            }, 1000);
        };

        startRedPhase();

        return () => {
            isComponentMounted.current = false;
            if (blinkTimeoutRef.current !== null) {
                window.clearTimeout(blinkTimeoutRef.current);
            }
        };
    }, [blinkCycleKey]);

    useEffect(() => {
        return () => {
            if (graceTimeoutRef.current !== null) {
                window.clearTimeout(graceTimeoutRef.current);
            }
        };
    }, []);

    if (!lineStationInfo || lineStationInfo.stations.length === 0) {
        return null;
    }

    const stations = lineStationInfo.stations;
    const lineColor = lineStationInfo.lineColor || 'rgb(255, 255, 255)';

    return (
        <div className="fpcc-stopstrip-container">
            <div className="fpcc-stopstrip-progress-track">
                <div className="fpcc-stopstrip-progress-bar-container">
                    <div className="fpcc-stopstrip-progress-bar" style={{ backgroundColor: lineColor }}></div>
                </div>
                {stations.map((station, index) => (
                    <div key={index} className="fpcc-stopstrip-station">
                        <div
                            className={`fpcc-stopstrip-station-dot ${index === blinkDotIndex && isBlinking ? 'blink-red' : ''
                                }`}
                        />
                        <div className="fpcc-stopstrip-station-name">{station.name}</div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default StopStripPanel;