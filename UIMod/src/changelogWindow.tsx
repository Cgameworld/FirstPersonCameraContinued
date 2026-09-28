import React, { useState, useRef, useCallback } from 'react';
import { trigger } from "cs2/api";
import { Scrollable } from "cs2/ui";
import engine from 'cohtml/cohtml';
import followRandomCustomImg from "images/followrandomcustomwindow.png";
import 'style/ChangelogWindow.scss';

// ---- EDIT CHANGELOG CONTENT HERE ----
const CHANGELOG_TITLE = "v1.7 Update!";

interface ChangelogItem {
    text: string;
    image?: string;
    imageWidth?: string;
    imageHeight?: string;
}

interface ChangelogSection {
    heading: string;
    items: ChangelogItem[];
}

const CHANGELOG_HIGHLIGHTS: ChangelogSection[] = [
    { heading: "Main New Features:", items: [
        { text: "Added custom follow random mode window where you can granularly select what to follow", image: followRandomCustomImg, imageWidth: '612rem', imageHeight: '137rem' },
        { text: "Added altitude information in infobox for aircraft"}
    ]},
];

const CHANGELOG_FULL: ChangelogSection[] = [
    { heading: "Improvements:", items: [
        { text: "Improved strip map stop name detection and filtering" },
        { text: "Improved follow camera for wildlife" },
        { text: "Improved backend random follow querying and vehicle type detection" },
        { text: "Improved dropdown menu with submenus and optional quick open keybindings" },
        { text: "Improved UI tab setting ordering" },
        { text: "Changed cim to citizen in UI text for vanilla consistency" },
        { text: "Updated translations" },
    ]},
    { heading: "Bug Fixes:", items: [
        { text: "Fixed bottom clamping issue after following citizens" },
        { text: "Fixed PIP view not closing when exiting first person mode while \"Show Game UI\" is turned on" },
        { text: "Fixed issue where FOV increased with fast transition speed" },
        { text: "Fixed issue where background city sounds became quieter each session" },
        { text: "Fixed speed info box width padding not rendering when following after free camera entry" },
        { text: "Fixed tooltip lingering on screen after entering follow mode from an entity info window" },
        { text: "Fixed missing vanilla UI sounds" },
        { text: "Fixed svg scaling issue" },
    ]},
];
// ---- END CHANGELOG CONTENT ----

const SectionList: React.FC<{ sections: ChangelogSection[] }> = ({ sections }) => (
    <>
        {sections.map((section, i) => (
            <div key={i} style={i > 0 ? { marginTop: '12rem' } : {}}>
                <p className="p_CKq" style={{ fontWeight: 'bold', marginBottom: '6rem', fontSize: '19rem' }}>{section.heading}</p>
                {section.items.map((item, j) => (
                    <div key={j}>
                        <p className="p_CKq" style={{ fontSize: '17rem', marginRight: '20rem' }}>- {item.text}</p>
                        {item.image && (
                            <img
                                src={item.image}
                                style={{
                                    width: item.imageWidth,
                                    height: item.imageHeight,
                                    marginTop: '6rem',
                                    marginBottom: '8rem',
                                }}
                            />
                        )}
                    </div>
                ))}
            </div>
        ))}
    </>
);

const ChangelogWindow: React.FC = () => {
    const [showMore, setShowMore] = useState(false);
    const [visible, setVisible] = useState(true);
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const dragRef = useRef<{ startX: number; startY: number; origX: number; origY: number } | null>(null);

    const playHoverSound = () => {
        engine.trigger("audio.playSound", "hover-item", 1);
    };

    const onClose = () => {
        engine.trigger("audio.playSound", "select-item", 1);
        setVisible(false);
        trigger("fpc", "DismissChangelog");
    };

    const onMouseDown = useCallback((e: React.MouseEvent) => {
        dragRef.current = {
            startX: e.clientX,
            startY: e.clientY,
            origX: position.x,
            origY: position.y,
        };

        const onMouseMove = (ev: MouseEvent) => {
            if (!dragRef.current) return;
            setPosition({
                x: dragRef.current.origX + (ev.clientX - dragRef.current.startX),
                y: dragRef.current.origY + (ev.clientY - dragRef.current.startY),
            });
        };

        const onMouseUp = () => {
            dragRef.current = null;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    }, [position]);

    if (!visible) return null;

    return (
        <div
            style={{
                position: "fixed",
                top: 0,
                left: 0,
                width: "100%",
                height: "85%",
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                zIndex: 9999,
                pointerEvents: "none",
            }}
        >
            <div
                className="panel_YqS error-dialog_iaV"
                style={{
                    maxWidth: "100%",
                    maxHeight: "100%",
                    width: '662rem',
                    pointerEvents: "auto",
                    transform: `translate(${position.x}px, ${position.y}px)`,
                }}
            >
                <div
                    className="header_jAe header_Bpo child-opacity-transition_nkS"
                    onMouseDown={onMouseDown}
                    style={{ cursor: 'grab' }}
                >
                    <div className="title-bar_PF4">
                        <div className="icon_VQU">
                            <img className="iconImg_ThV" src="coui://uil/Standard/VideoCamera.svg" />
                        </div>
                        <div className="icon-space_h_f"></div>
                        <div className="title_SVH title_zQN">First Person Camera Continued</div>
                        <button className="button_bvQ button_bvQ close-button_wKK" onMouseEnter={playHoverSound} onClick={onClose}>
                            <div className="tinted-icon_iKo icon_PhD" style={{ maskImage: "url(Media/Glyphs/Close.svg)" }}></div>
                        </button>
                    </div>
                </div>
                <div className="content_VBF content_AD7 child-opacity-transition_nkS">
                    <div className="icon-layout_cZT row_L6K">
                        <div className="main-column_Jzk">
                            <div className="error-message_r4_" style={{marginTop: '-4rem', marginRight: '-12rem'}}>
                                <div className="paragraphs_nbD" style={{ padding: '8rem' }}>
                                    <Scrollable vertical trackVisibility="scrollable" className="fpc-changelog-scrollable" style={{ marginRight: '-3rem' }}>
                                    <p className="p_CKq" style={{ fontSize: '21rem', fontWeight: 'bold', marginBottom: '12rem' }}>{CHANGELOG_TITLE}</p>
                                    <SectionList sections={CHANGELOG_HIGHLIGHTS} />

                                    <div style={{ marginTop: '14rem', marginRight: '20rem' }}>
                                        <button
                                            className="button_HeP button_gJo"
                                            style={{ width: 'auto', padding: '5rem 14rem'}}
                                            onMouseEnter={playHoverSound}
                                            onClick={() => {
                                                engine.trigger("audio.playSound", "select-item", 1);
                                                setShowMore(!showMore);
                                            }}
                                        >
                                            {showMore ? 'Hide Full Changelog ▲' : 'Show Full Changelog ▼'}
                                        </button>
                                    </div>

                                    {showMore && (
                                        <div style={{ marginTop: '12rem' }}>
                                            <SectionList sections={CHANGELOG_FULL} />
                                        </div>
                                    )}
                                    </Scrollable>
                                </div>
                            </div>
                            <div className="buttons-container" style={{ marginTop: '22rem', marginRight: '12rem', textAlign: 'right' }}>
                                <div className="buttons_lZi row_L6K" style={{ width: '175rem' }}>
                                    <button className="button_HeP button_gJo" style={{ width: "130rem"}} onMouseEnter={playHoverSound} onClick={onClose}>Ok</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ChangelogWindow;
