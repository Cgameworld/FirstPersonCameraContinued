import React, { useState, useEffect, useRef, useMemo } from 'react';
import { bindValue, useValue } from "cs2/api";
import 'style/StopStripPanel.scss';

const SLIDING_WINDOW_THRESHOLD = 14;
const SLIDING_WINDOW_SIZE = 13;

interface StationData {
    name: string;
}

interface LineStationInfo {
    stations: StationData[];
    currentStopIndex: number;
    lineColor: string;
}

const LineStationInfo$ = bindValue<string>('fpc', 'LineStationInfo');
const UISettingsGroupOptions$ = bindValue<string>('fpc', 'UISettingsGroupOptions');

const StopStripPanel: React.FC = () => {
    const [blinkDotIndex, setBlinkDotIndex] = useState(-1);
    const [isBlinking, setIsBlinking] = useState(false);
    const [blinkCycleKey, setBlinkCycleKey] = useState(0);
    const [isPanelVisible, setIsPanelVisible] = useState(false);
    const blinkTimeoutRef = useRef<number | null>(null);
    const isComponentMounted = useRef(true);
    const canStartNewBlinkRef = useRef(true);
    const graceTimeoutRef = useRef<number | null>(null);
    const blinkDotIndexRef = useRef(-1);
    const lastStopNameRef = useRef<string>("");
    const wasAtStationRef = useRef(false);
    const hideTimeoutRef = useRef<number | null>(null);
    const lastValidInfoRef = useRef<LineStationInfo | null>(null);
    const lastWindowCenterRef = useRef(0);
    const containerRef = useRef<HTMLDivElement>(null);
    const slideAnimRef = useRef<number | null>(null);

    const lineStationInfoStr = useValue(LineStationInfo$);
    const uiSettingsGroupOptions = useValue(UISettingsGroupOptions$);

    const stopStripDisplayMode: number = React.useMemo(() => {
        try {
            return JSON.parse(uiSettingsGroupOptions).StopStripDisplayMode ?? 0;
        } catch {
            return 0;
        }
    }, [uiSettingsGroupOptions]);

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

    if (lineStationInfo && lineStationInfo.stations.length > 0) {
        lastValidInfoRef.current = lineStationInfo;
    }

    useEffect(() => {
        if (!lineStationInfo) {
            wasAtStationRef.current = false;
            blinkDotIndexRef.current = -1;
            setBlinkDotIndex(-1);
            lastStopNameRef.current = "";
            canStartNewBlinkRef.current = true;

            if (hideTimeoutRef.current !== null) {
                window.clearTimeout(hideTimeoutRef.current);
                hideTimeoutRef.current = null;
            }
            setIsPanelVisible(false);
            return;
        }

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

            if (hideTimeoutRef.current !== null) {
                window.clearTimeout(hideTimeoutRef.current);
                hideTimeoutRef.current = null;
            }
            setIsPanelVisible(true);
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

            if (stopStripDisplayMode === 0 && hideTimeoutRef.current === null) {
                hideTimeoutRef.current = window.setTimeout(() => {
                    setIsPanelVisible(false);
                    hideTimeoutRef.current = null;
                }, 2000);
            }
        }

        if (stopStripDisplayMode === 1 && lineStationInfo.stations.length > 0) {
            setIsPanelVisible(true);
            if (hideTimeoutRef.current !== null) {
                window.clearTimeout(hideTimeoutRef.current);
                hideTimeoutRef.current = null;
            }
        }
    }, [lineStationInfo, stopStripDisplayMode]);

    useEffect(() => {
        const el = containerRef.current;
        if (!el) return;

        const isVisible = isPanelVisible || stopStripDisplayMode === 1;
        const targetY = isVisible ? 0 : 100;

        if (slideAnimRef.current !== null) {
            cancelAnimationFrame(slideAnimRef.current);
            slideAnimRef.current = null;
        }

        const startY = parseFloat(el.style.transform?.match(/translateY\(([\d.]+)/)?.[1] ?? '100');
        if (Math.abs(startY - targetY) < 0.1) {
            el.style.transform = `translateY(${targetY}%)`;
            return;
        }

        const startTime = performance.now();
        const duration = 375;
        const animate = (now: number) => {
            const t = Math.min((now - startTime) / duration, 1);
            el.style.transform = `translateY(${startY + (targetY - startY) * t}%)`;
            if (t < 1) {
                slideAnimRef.current = requestAnimationFrame(animate);
            } else {
                slideAnimRef.current = null;
            }
        };

        slideAnimRef.current = requestAnimationFrame(animate);
    }, [isPanelVisible, stopStripDisplayMode]);

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
            if (hideTimeoutRef.current !== null) {
                window.clearTimeout(hideTimeoutRef.current);
            }
            if (slideAnimRef.current !== null) {
                cancelAnimationFrame(slideAnimRef.current);
            }
        };
    }, []);

    const renderData = (lineStationInfo && lineStationInfo.stations.length > 0)
        ? lineStationInfo
        : lastValidInfoRef.current;

    const stationCount = renderData?.stations.length ?? 0;
    const useWindow = stationCount > SLIDING_WINDOW_THRESHOLD;

    if (blinkDotIndex >= 0) {
        lastWindowCenterRef.current = blinkDotIndex;
    }

    const windowStart = useMemo(() => {
        if (!useWindow) return 0;
        const center = lastWindowCenterRef.current;
        const half = Math.floor(SLIDING_WINDOW_SIZE / 2);
        const maxStart = stationCount - SLIDING_WINDOW_SIZE;
        return Math.max(0, Math.min(center - half, maxStart));
    }, [useWindow, blinkDotIndex, stationCount]);

    if (!renderData || stationCount === 0) {
        return null;
    }

    const stations = renderData.stations;
    const lineColor = renderData.lineColor || 'rgb(255, 255, 255)';

    const visibleStations = useWindow
        ? stations.slice(windowStart, windowStart + SLIDING_WINDOW_SIZE)
        : stations;

    const showLeftArrow = useWindow && windowStart > 0;
    const showRightArrow = useWindow && windowStart + SLIDING_WINDOW_SIZE < stations.length;

    const trackClasses = [
        'fpcc-stopstrip-progress-track',
        showLeftArrow ? 'has-left-arrow' : '',
        showRightArrow ? 'has-right-arrow' : '',
    ].filter(Boolean).join(' ');

    return (
        <div ref={containerRef} className="fpcc-stopstrip-container tool-options-panel_Se6">
            <div className={trackClasses}>
                <div className="fpcc-stopstrip-progress-bar-container">
                    <div className="fpcc-stopstrip-progress-bar" style={{ backgroundColor: lineColor }}></div>
                </div>
                {showLeftArrow && (
                    <div className="fpcc-stopstrip-arrow fpcc-stopstrip-arrow-left">
                        <svg viewBox="0 0 24 24" width="38rem" height="38rem">
                            <polygon points="17,2 5,12 17,22" fill={lineColor} stroke="none"/>
                        </svg>
                    </div>
                )}
                {visibleStations.map((station, i) => {
                    const realIndex = windowStart + i;
                    return (
                        <div key={realIndex} className="fpcc-stopstrip-station">
                            <div
                                className={`fpcc-stopstrip-station-dot ${realIndex === blinkDotIndex && isBlinking ? 'blink-red' : ''
                                    }`}
                            />
                            <div className="fpcc-stopstrip-station-name">{station.name}</div>
                        </div>
                    );
                })}
                {showRightArrow && (
                    <div className="fpcc-stopstrip-arrow fpcc-stopstrip-arrow-right">
                        <svg viewBox="0 0 24 24" width="38rem" height="38rem">
                            <polygon points="7,2 19,12 7,22" fill={lineColor} stroke="none"/>
                        </svg>
                    </div>
                )}
            </div>
        </div>
    );
};

export default StopStripPanel;
