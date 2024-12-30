import React, { useState, useEffect, useRef } from 'react';
import 'style/StopStripPanel.scss';

const StopStripPanel: React.FC = () => {
    const [currentStop, setCurrentStop] = useState(0);
    const [isBlinking, setIsBlinking] = useState(false);
    const blinkTimeoutRef = useRef<number | null>(null);
    const isComponentMounted = useRef(true);

    useEffect(() => {
        // Set mounted flag
        isComponentMounted.current = true;

        const startBlinking = () => {
            if (!isComponentMounted.current) return;

            setIsBlinking(true);

            // Use timeout instead of interval for more precise control
            blinkTimeoutRef.current = window.setTimeout(() => {
                if (isComponentMounted.current) {
                    setIsBlinking(false);
                    // Schedule next blink
                    blinkTimeoutRef.current = window.setTimeout(startBlinking, 1000);
                }
            }, 1000);
        };

        startBlinking();

        // Cleanup function
        return () => {
            isComponentMounted.current = false;
            if (blinkTimeoutRef.current !== null) {
                window.clearTimeout(blinkTimeoutRef.current);
            }
        };
    }, []);

    /*
    const stations = [
        { name: 'Elk Grove' },
        { name: 'Lindbergh/Lafayette' },
        { name: 'Lindbergh/Myrtle' },
        { name: 'Vermont/Hawthorne' },
        { name: 'Lake/Beech' },
        { name: 'Bedford/Manor Highway' },
        { name: 'Briar Rose/Foggy' },
    ]
    */

    const stations = [
        { name: '188 Elk Grove' },
        { name: '201 Riverside Park' },
        { name: '156 Cedar Hills' },
        { name: '342 Maple Valley' },
        { name: '275 Pinecrest' },
        { name: '433 Highland Junction' },
        { name: '167 Oakwood' },
        { name: '299 Willow Springs' },
        { name: '544 Evergreen Plaza' },
        { name: '122 Redwood Heights' },
        { name: '398 Birchwood' },
        { name: '543 Aspen Grove' },
        { name: '455 Sycamore Square' },
        { name: '321 Magnolia Park' },
        { name: '177 Cherry Blossom' }
    ];
    return (
        <div className="fpcc-stopstrip-container">
            <div className="fpcc-stopstrip-progress-track">
                <div className="fpcc-stopstrip-progress-bar-container">
                    <div className="fpcc-stopstrip-progress-bar"></div>
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