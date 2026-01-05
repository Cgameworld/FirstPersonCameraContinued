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
    const [currentStop, setCurrentStop] = useState(0);
    const [isBlinking, setIsBlinking] = useState(false);
    const blinkTimeoutRef = useRef<number | null>(null);
    const isComponentMounted = useRef(true);

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
        if (lineStationInfo) {
            setCurrentStop(lineStationInfo.currentStopIndex);
        }
    }, [lineStationInfo]);

    useEffect(() => {
        isComponentMounted.current = true;

        const startBlinking = () => {
            if (!isComponentMounted.current) return;

            setIsBlinking(true);

            blinkTimeoutRef.current = window.setTimeout(() => {
                if (isComponentMounted.current) {
                    setIsBlinking(false);
                    blinkTimeoutRef.current = window.setTimeout(startBlinking, 1000);
                }
            }, 1000);
        };

        startBlinking();

        return () => {
            isComponentMounted.current = false;
            if (blinkTimeoutRef.current !== null) {
                window.clearTimeout(blinkTimeoutRef.current);
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
                            className={`fpcc-stopstrip-station-dot ${index === currentStop && isBlinking ? 'blink-red' : ''
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